using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Muddlr.Api;
using Muddlr.Persons;
using Muddlr.WebFinger;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Muddlr.Api.HealthStatus;

namespace Muddlr.Test;

public class PersonApiTests: IDisposable
{
    private readonly WebApplicationFactory<Program> _app;
    private readonly HttpClient _client;
    private readonly string _tempFileName;

    private readonly Person _matt = new Person()
    {
        Name = "Matt Test", 
        FediverseHandle = "tester", 
        FediverseServer = "test.social", 
        Locators = new HashSet<string> {"tester@thetest.com"},
        Aliases = new HashSet<Uri>
            {new("https://test.social/@tester"), new("https://test.social/users/tester")},
        Links = new List<WebFingerLink>
        {
            new WebFingerLink
            {
                Relationship = Relationship.WebFingerProfile, Type = LinkType.TextHtml,
                Href = new("https://test.social/@tester")
            },
            new WebFingerLink
            {
                Relationship = Relationship.Self, Type = LinkType.ApplicationActivityJson,
                Href = new("https://test.social/users/tester")
            },
            new WebFingerLink
            {
                Relationship = Relationship.OStatusSubscribe,
                Template = "https://test.social/authorize_interaction?uri={uri}"
            }
        }
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public PersonApiTests()
    {
        _tempFileName = Path.GetTempFileName();
        var personRepo = new LiteDbDataSource(_tempFileName);
        
        personRepo.AddPerson(_matt);
        _app = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder
                .ConfigureServices(services =>
                {
                    services.AddSingleton<IPersonRepository>(personRepo);
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
        
        var expected = new MuddlrStatus {ApiVersion = apiVersionText, CoreVersion = coreVersionText, Status = HealthStatus.Ok};

        var response = await _client.GetAsync("/health");
        var health = await response.Content.ReadFromJsonAsync<MuddlrStatus>(JsonOptions);

        health.Should().NotBeNull();
        health.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task PersonRouteReturnsAllPeople()
    {
        var response = await _client.GetAsync("/api/person");
        var people = await response.Content.ReadFromJsonAsync<PersonDto[]>();

        people.Should().NotBeNull();
        people.Should().NotBeEmpty();
        people!.Length.Should().Be(1);
    }

    [Fact]
    public async Task GetPersonByIdReturnsCorrectPerson()
    {
        var hashId = IdHasher.Instance.EncodeLong(1);
        var response = await _client.GetAsync($"/api/person/{hashId}");
        var matt = await response.Content.ReadFromJsonAsync<PersonDto>();
        matt.Should().NotBeNull();
        matt!.Name.Should().Be("Matt Test");
        matt!.Links.Should().BeEquivalentTo(_matt.Links);
        matt!.Locators.Should().BeEquivalentTo(_matt.Locators);
        matt!.Aliases.Should().BeEquivalentTo(_matt.Aliases);
    }

    [Fact]
    public async Task AddPersonUpdatesRepositoryWithCorrectFediverseLinks()
    {
        var person = new UpsertPersonDto(
            "John Test",
            new[] {"tester@thetest.com"}, 
            "jt", 
            "test.social");
        
        var expectedLinks = new List<WebFingerLink>
        {
            new WebFingerLink
            {
                Relationship = Relationship.WebFingerProfile, 
                Type = LinkType.TextHtml,
                Href = new("https://test.social/@jt")
            },
            new WebFingerLink
            {
                Relationship = Relationship.Self, 
                Type = LinkType.ApplicationActivityJson,
                Href = new("https://test.social/users/jt")
            },
            new WebFingerLink
            {
                Relationship = Relationship.OStatusSubscribe,
                Template = "https://test.social/authorize_interaction?uri={uri}"
            }
        };

        var response = await _client.PostAsJsonAsync("/api/person/", person);
        var result = await response.Content.ReadFromJsonAsync<PersonDto>();

        result.Should().NotBeNull();
        result!.Links.Should().BeEquivalentTo(expectedLinks);
    }

    public void Dispose()
    {
        if (File.Exists(_tempFileName))
        {
            File.Delete(_tempFileName);
        }
        
        _app.Dispose();
        _client.Dispose();
    }
}
