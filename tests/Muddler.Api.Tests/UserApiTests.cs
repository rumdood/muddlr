using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Muddlr.Api;
using Muddlr.Users;
using Muddlr.WebFinger;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Muddlr.Api.HealthStatus;

namespace Muddlr.Test;

public class UserApiTests: IDisposable
{
    private readonly WebApplicationFactory<Program> _app;
    private readonly HttpClient _client;
    private readonly string _tempFileName;

    private readonly User _matt = new User()
    {
        Name = "Matt Test", 
        FediverseAccount = new() { Server = "test.social", Username = "tester" },
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

    public UserApiTests()
    {
        _tempFileName = Path.GetTempFileName();
        var userRepo = new LiteDbDataSource(_tempFileName);
        
        userRepo.AddUser(_matt);
        _app = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder
                .ConfigureServices(services =>
                {
                    services.AddSingleton<IUserRepository>(userRepo);
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
        
        var coreVersion = typeof(User).Assembly.GetName().Version;
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
    public async Task UserRouteReturnsAllPeople()
    {
        var response = await _client.GetAsync("/api/user");
        var people = await response.Content.ReadFromJsonAsync<UserDto[]>();

        people.Should().NotBeNull();
        people.Should().NotBeEmpty();
        people!.Length.Should().Be(1);
    }

    [Fact]
    public async Task GetUserByIdReturnsCorrectUser()
    {
        var hashId = IdHasher.Instance.EncodeLong(1);
        var response = await _client.GetAsync($"/api/user/{hashId}");
        var matt = await response.Content.ReadFromJsonAsync<UserDto>();
        matt.Should().NotBeNull();
        matt!.Name.Should().Be("Matt Test");
        matt!.Links.Should().BeEquivalentTo(_matt.Links);
        matt!.Locators.Should().BeEquivalentTo(_matt.Locators);
        matt!.Aliases.Should().BeEquivalentTo(_matt.Aliases);
    }

    [Fact]
    public async Task AddUserUpdatesRepositoryWithCorrectFediverseLinks()
    {
        var user = new UpsertUserDto(
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

        var response = await _client.PostAsJsonAsync("/api/user/", user);
        var result = await response.Content.ReadFromJsonAsync<UserDto>();

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
