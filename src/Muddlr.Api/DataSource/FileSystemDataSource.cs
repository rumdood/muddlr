using Muddlr.Persons;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Muddlr.Api;

public class FileSystemDataSource: IPersonRepository
{
    private class RepositoryMeta
    {
        private long _maxId = 0;
        
        public long MaxId { get; init; }
        public long GetNextId() => ++_maxId;
    }
    
    private const string People = "People";
    private const string Fault = "Fault";

    private readonly ILogger<FileSystemDataSource> _logger;
    private readonly string _folder;
    private readonly string _faultedFolder;

    private readonly Dictionary<long, Person> _personById = new();
    private readonly Dictionary<string, Person> _personByLocator = new();
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

        _folder = Path.Combine(env.ContentRootPath, People);
        _faultedFolder = Path.Combine(_folder, Fault);
        _logger = logger;

        LoadPeople();
        _meta = new RepositoryMeta { MaxId = _personById.Any() ? _personById.Keys.Max() : 0 };
    }

    private void LoadPeople()
    {
        if (!Directory.Exists(_folder))
        {
            Directory.CreateDirectory(_folder);
        }

        var faulted = new List<string>();

        foreach (var file in Directory.EnumerateFiles(_folder, "*.json", SearchOption.TopDirectoryOnly))
        {
            var json = File.ReadAllText(file);
            var person = JsonSerializer.Deserialize<Person>(json, JsonOptions);

            if (person is null)
            {
                _logger.LogError("Failed to index person in {File}", file);
                faulted.Add(file);
                continue;
            }

            _personById[person.Id] = person;
            foreach (var locator in person.Locators)
            {
                if (_personByLocator.TryAdd(locator, person))
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

    private void SavePersonFile(Person person)
    {
        if (person is null)
        {
            throw new ArgumentNullException(nameof(person));
        }

        var fileName = $"{person.Id}.json";
        var filePath = Path.Combine(_folder, fileName);

        var buffer = JsonSerializer.SerializeToUtf8Bytes(person, JsonOptions);
        File.WriteAllBytes(filePath, buffer);
    }

    public Person? GetPerson(PersonFilter filter)
    {
        return filter switch
        {
            { Id: var id and > 0 } => _personById[id],
            { Id: 0, Locator: var locator, Relationships: {Length: 0}} when !string.IsNullOrEmpty(locator) =>
                _personByLocator[locator],
            { Id: 0, Locator: var locator, Relationships: var relationFilters } when !string.IsNullOrEmpty(locator) => 
                _personByLocator[locator].WithFilteredLinks(relationFilters),
            _ => null
        };
    }

    public List<Person> GetAllPersons() => _personById.Values.ToList();

    public AddPersonResult AddPerson(Person person)
    {
        var nextId = _meta.GetNextId();
        var personWithId = person.WithId(nextId);

        try
        {
            SavePersonFile(personWithId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save Person with Id {Id}", nextId);
            return new AddPersonResult(false, "Failed To Insert, Check Log");
        }
        
        var success = _personById.TryAdd(nextId, personWithId);

        if (success)
        {
            foreach (var locator in person.Locators)
            {
                success = _personByLocator.TryAdd(locator, _personById[nextId]);

                if (!success)
                {
                    break;
                }
            }
        }

        if (!success && _personById.TryGetValue(nextId, out var added))
        {
            foreach (var locator in added.Locators)
            {
                _ = _personByLocator.Remove(locator);
            }

            _personById.Remove(nextId);
        }

        return new AddPersonResult(
            true,
            success ? "Person Added" : "Person Added With Indexing Errors",
            success ? _personById[nextId] : null);
    }

    public UpdatePersonResult UpdatePerson(Person person)
    {
        if (!_personById.TryGetValue(person.Id, out var existing))
        {
            return new UpdatePersonResult(false, $"Person with Id of {0} not found");
        }

        foreach (var deadLocator in existing.Locators.Where(loc => !person.Locators.Contains(loc)))
        {
            _ = _personByLocator.Remove(deadLocator);
        }

        foreach (var locator in person.Locators)
        {
            _personByLocator[locator] = person;
        }

        _personById[person.Id] = person;

        return new UpdatePersonResult(true, "Updated Person Record");
    }

    public bool DeletePerson(Person person)
    {
        if (!_personById.TryGetValue(person.Id, out var existing))
        {
            return false;
        }

        foreach (var locator in person.Locators)
        {
            _ = _personByLocator.Remove(locator);
        }

        _personById.Remove(person.Id);
        return true;
    }
}