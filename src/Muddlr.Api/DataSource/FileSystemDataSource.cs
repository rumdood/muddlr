using Muddlr.Users;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Muddlr.Api;

public class FileSystemDataSource: IUserRepository
{
    private class RepositoryMeta
    {
        private long _maxId = 0;
        
        public long MaxId { get; init; }
        public long GetNextId() => ++_maxId;
    }

    private const string DataRoot = ".data";
    private const string Users = "Users";
    private const string Fault = "Fault";

    private readonly ILogger<FileSystemDataSource> _logger;
    private readonly string _folder;
    private readonly string _faultedFolder;

    private readonly Dictionary<long, User> _userById = new();
    private readonly Dictionary<string, User> _userByLocator = new();
    private readonly RepositoryMeta _meta;
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    public FileSystemDataSource(IWebHostEnvironment env, ILogger<FileSystemDataSource> logger)
    {
        if (env is null)
        {
            throw new ArgumentNullException(nameof(env));
        }

        _folder = Path.Combine(env.ContentRootPath, DataRoot, Users);
        _faultedFolder = Path.Combine(_folder, Fault);
        _logger = logger;

        LoadUsers();
        _meta = new RepositoryMeta { MaxId = _userById.Any() ? _userById.Keys.Max() : 0 };
    }

    private void LoadUsers()
    {
        if (!Directory.Exists(_folder))
        {
            Directory.CreateDirectory(_folder);
        }

        var faulted = new List<string>();

        foreach (var file in Directory.EnumerateFiles(_folder, "*.json", SearchOption.TopDirectoryOnly))
        {
            var json = File.ReadAllText(file);
            var user = JsonSerializer.Deserialize<User>(json, JsonOptions);

            if (user is null)
            {
                _logger.LogError("Failed to index user in {File}", file);
                faulted.Add(file);
                continue;
            }

            _userById[user.Id] = user;
            foreach (var locator in user.Locators)
            {
                if (_userByLocator.TryAdd(locator, user))
                {
                    continue;
                }
                
                _logger.LogWarning("Failed to update locator index for {File}", file);
            }
        }
        
        // move faulted files so they won't be indexed again
        foreach (var file in faulted)
        {
            File.Move(file, Path.Combine(_faultedFolder, Path.GetFileName(file)));
        }
    }

    private void SaveuserFile(User user)
    {
        if (user is null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        var fileName = $"{user.Id}.json";
        var filePath = Path.Combine(_folder, fileName);

        var buffer = JsonSerializer.SerializeToUtf8Bytes(user, JsonOptions);
        File.WriteAllBytes(filePath, buffer);
    }

    public User? GetUser(UserFilter filter)
    {
        return filter switch
        {
            { Id: var id and > 0 } => _userById.TryGetValue(id, out var user) ? user : null,
            { Id: 0, Locator: var locator, Relationships: {Length: 0}} when !string.IsNullOrEmpty(locator) =>
                _userByLocator.TryGetValue(locator, out var user) ? user : null,
            { Id: 0, Locator: var locator, Relationships: var relationFilters } when !string.IsNullOrEmpty(locator) => 
                _userByLocator.TryGetValue(locator, out var user) ? user.WithFilteredLinks(relationFilters) : null,
            _ => null
        };
    }

    public IEnumerable<User> GetAllUsers() => _userById.Values;

    public AddUserResult AddUser(User user)
    {
        var nextId = _meta.GetNextId();
        var userWithId = user.WithId(nextId);

        try
        {
            SaveuserFile(userWithId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save user with Id {Id}", nextId);
            return new AddUserResult(false, "Failed To Insert, Check Log");
        }
        
        var success = _userById.TryAdd(nextId, userWithId);

        if (success)
        {
            foreach (var locator in user.Locators)
            {
                success = _userByLocator.TryAdd(locator, _userById[nextId]);

                if (!success)
                {
                    break;
                }
            }
        }

        if (!success && _userById.TryGetValue(nextId, out var added))
        {
            foreach (var locator in added.Locators)
            {
                _ = _userByLocator.Remove(locator);
            }

            _userById.Remove(nextId);
        }

        return new AddUserResult(
            true,
            success ? "user Added" : "user Added With Indexing Errors",
            success ? _userById[nextId] : null);
    }

    public UpdateUserResult UpdateUser(User user)
    {
        if (!_userById.TryGetValue(user.Id, out var existing))
        {
            return new UpdateUserResult(false, $"user with Id of {0} not found");
        }

        foreach (var deadLocator in existing.Locators.Where(loc => !user.Locators.Contains(loc)))
        {
            _ = _userByLocator.Remove(deadLocator);
        }

        foreach (var locator in user.Locators)
        {
            _userByLocator[locator] = user;
        }

        _userById[user.Id] = user;

        return new UpdateUserResult(true, "Updated user Record");
    }

    public bool DeleteUser(User user)
    {
        if (!_userById.TryGetValue(user.Id, out var existing))
        {
            return false;
        }

        foreach (var locator in user.Locators)
        {
            _ = _userByLocator.Remove(locator);
        }

        _userById.Remove(user.Id);
        return true;
    }
}