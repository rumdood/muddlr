using Muddlr.Persons;

namespace Muddlr.Test;

internal class InMemoryPersonRepository : IPersonRepository
{
    private readonly Dictionary<string, Person> _peopleByLocator = new Dictionary<string, Person>();
    private readonly Dictionary<long, Person> _peopleById = new Dictionary<long, Person>();

    public Person? GetPerson(PersonFilter filter)
    {
        return filter switch
        {
            { Id: var id, Locator: var locator } when id > 0 => _peopleById[id],
            {Id: 0, Locator: var locator, Relationships: {Length: 0}} when !string.IsNullOrEmpty(locator) =>
                _peopleByLocator[locator],
            { Id: 0, Locator: var locator, Relationships: var rels } when !string.IsNullOrEmpty(locator) => 
                _peopleByLocator.ToDictionary(
                    kv => kv.Key, 
                    kv => new Person
                    {
                        Id = kv.Value.Id,
                        Name = kv.Value.Name,
                        Email = kv.Value.Email,
                        Locators = kv.Value.Locators,
                        Aliases = kv.Value.Aliases,
                        FediverseHandle = kv.Value.FediverseHandle,
                        FediverseServer = kv.Value.FediverseServer,
                        Links = kv.Value.Links
                            .EmptyIfNull()
                            .Where(link => rels.Contains(link.Relationship))
                            .ToList()
                    })[locator],
            _ => null
        };
    }

    public List<Person> GetAllPersons() => _peopleById.Values.ToList();

    public AddPersonResult AddPerson(Person person)
    {
        var nextId = _peopleById.Any() ? _peopleById.Keys.Max() + 1 : 1;
        var success = _peopleById.TryAdd(nextId, new Person
        {
            Id = nextId,
            Name = person.Name,
            Email = person.Email,
            Locators = person.Locators,
            Aliases = person.Aliases,
            FediverseHandle = person.FediverseHandle,
            FediverseServer = person.FediverseServer,
            Links = person.Links,
        });

        if (success)
        {
            foreach (var locator in person.Locators)
            {
                success = _peopleByLocator.TryAdd(locator, _peopleById[nextId]);

                if (!success)
                {
                    break;
                }
            }
        }

        if (!success && _peopleById.TryGetValue(nextId, out var added))
        {
            foreach (var locator in added.Locators)
            {
                _ = _peopleByLocator.Remove(locator);
            }

            _peopleById.Remove(nextId);
        }

        return new AddPersonResult(
            success, 
            success ? "Person Added" : "Failed To Insert",
            success ? _peopleById[nextId] : null);
    }

    public UpdatePersonResult UpdatePerson(Person person)
    {
        if (!_peopleById.TryGetValue(person.Id, out var existing))
        {
            return new UpdatePersonResult(false, $"Person with Id of {0} not found");
        }

        foreach (var deadLocator in existing.Locators.Where(loc => !person.Locators.Contains(loc)))
        {
            _ = _peopleByLocator.Remove(deadLocator);
        }

        foreach (var locator in person.Locators)
        {
            _peopleByLocator[locator] = person;
        }

        _peopleById[person.Id] = person;

        return new UpdatePersonResult(true, "Updated Person Record");
    }

    public bool DeletePerson(Person person)
    {
        if (!_peopleById.TryGetValue(person.Id, out var existing))
        {
            return false;
        }

        foreach (var locator in person.Locators)
        {
            _ = _peopleByLocator.Remove(locator);
        }

        _peopleById.Remove(person.Id);
        return true;
    }
}