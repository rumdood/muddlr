using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Muddlr.Api;
using Muddlr.Persons;
using Muddlr.WebFinger;
using System.Net.Http.Json;

namespace Muddlr.Test;

public class PersonApiTests: IDisposable
{
    private readonly WebApplicationFactory<Program> _app;
    private readonly HttpClient _client;
    private readonly InMemoryPersonRepository _personRepo = new();

    public PersonApiTests()
    {
        _personRepo.AddPerson(
            new Person
            {
                Name = "Matt Test",
                Email = "mtest@test.com",
                FediverseHandle = "tester",
                FediverseServer = "test.social",
                Locators = new HashSet<string> {"tester@thetest.com"},
                Aliases = new HashSet<Uri>
                    {new("https://test.social/@tester"), new("https://test.social/users/tester")},
                Links = new List<WebFingerLink>
                {
                    new WebFingerLink
                    {
                        Relationship = Relationships.WebFingerProfile, Type = LinkTypes.Text.Html,
                        Href = new("https://test.social/@tester")
                    },
                    new WebFingerLink
                    {
                        Relationship = Relationships.Self, Type = LinkTypes.Application.ActivityJson,
                        Href = new("https://test.social/users/tester")
                    },
                    new WebFingerLink
                    {
                        Relationship = Relationships.OStatusSubscribe,
                        Template = "https://test.social/authorize_interaction?uri={uri}"
                    }
                },
            });
        _app = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder
                .ConfigureServices(services =>
                {
                    services.AddSingleton<IPersonRepository>(_personRepo);
                }));

        _client = _app.CreateClient();
    }

    [Fact]
    public async Task HealthRouteReturnsMuddlrStatusWithVersion()
    {
        var apiVersion = typeof(Program).Assembly.GetName().Version;
        var apiVersionText = apiVersion is not null
            ? apiVersion.ToString()
            : "UNK";
        
        var coreVersion = typeof(Person).Assembly.GetName().Version;
        var coreVersionText = coreVersion is not null
            ? coreVersion.ToString()
            : "UNK";
        
        var expected = $"\"Muddlr API Version: {apiVersionText}, Core Version: {coreVersionText} OK\""; // yes, we'll need more statuses

        var response = await _client.GetStringAsync("/health");

        response.Should().Be(expected);
    }

    [Fact]
    public async Task PersonRouteReturnsAllPeople()
    {
        var response = await _client.GetAsync("/api/person");
        var people = await response.Content.ReadFromJsonAsync<Person[]>();

        people.Should().NotBeNull();
        people.Should().NotBeEmpty();
        people!.Length.Should().Be(1);
    }

    [Fact]
    public async Task GetPersonByIdReturnsCorrectPerson()
    {
        const long id = 1;
        var response = await _client.GetAsync($"/api/person/{id}");
        var matt = await response.Content.ReadFromJsonAsync<Person>();
        matt.Should().NotBeNull();
        matt!.Name.Should().Be("Matt Test");
    }

    [Fact]
    public async Task AddPersonUpdatesRepositoryWithCorrectFediverseLinks()
    {
        var person = new PersonDto(
            "John Test", 
            "jt@thetest.com", 
            new[] {"tester@thetest.com"}, "jt", "test.social");
        
        var expectedLinks = new List<WebFingerLink>
        {
            new WebFingerLink
            {
                Relationship = Relationships.WebFingerProfile, Type = LinkTypes.Text.Html,
                Href = new("https://test.social/@jt")
            },
            new WebFingerLink
            {
                Relationship = Relationships.Self, Type = LinkTypes.Application.ActivityJson,
                Href = new("https://test.social/users/jt")
            },
            new WebFingerLink
            {
                Relationship = Relationships.OStatusSubscribe,
                Template = "https://test.social/authorize_interaction?uri={uri}"
            }
        };

        var response = await _client.PostAsJsonAsync("/api/person/new", person);
        var result = await response.Content.ReadFromJsonAsync<Person>();

        result.Should().NotBeNull();
        result!.Links.Should().BeEquivalentTo(expectedLinks);
    }

    public void Dispose()
    {
        _app.Dispose();
        _client.Dispose();
    }
}
