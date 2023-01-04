using Muddlr.Persons;
using Humanizer;
using LiteDB;

namespace Muddlr.Api;

internal class LiteDbDataSource: IPersonRepository
{
    private readonly string _connectionString;

    public LiteDbDataSource(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    private LiteDatabase GetDatabaseContext()
    {
        return new LiteDatabase(_connectionString);
    }
    
    private static void EnsureIndexes(ILiteCollection<Person> collection)
    {
        collection.EnsureIndex(u => u.Id);
        collection.EnsureIndex(u => u.Locators);
        collection.EnsureIndex("AliasesByRel", "LOWER($.Aliases[*].Relationship)");
    }

    public List<Person> GetAllPersons()
    {
        using var db = GetDatabaseContext();
        var personCollection = db.GetCollectionWithPlural<Person>();
        return personCollection.Query().ToList();
    }

    public Person? GetPerson(PersonFilter filter)
    {
        using var db = GetDatabaseContext();
        var personCollection = db.GetCollectionWithPlural<Person>();
        var person = string.IsNullOrEmpty(filter.Locator)
            ? personCollection.Query()
                .Where(x => x.Id == filter.Id)
                .SingleOrDefault()
            : personCollection.Query()
                .Where(x => x.Locators.Contains(filter.Locator))
                .SingleOrDefault();

        return person is not null && filter.Relationships.Any() 
            ? person.WithFilteredLinks(filter.Relationships)
            : person;
    }

    public AddPersonResult AddPerson(Person person)
    {
        try
        { 
            using var db = GetDatabaseContext();
            var personCollection = db.GetCollectionWithPlural<Person>();
            _ = personCollection.Insert(person);

            EnsureIndexes(personCollection);

            var inserted = personCollection.Query()
                .Where(x => x.Locators == person.Locators)
                .Single();

            return new AddPersonResult(true, "Person added", inserted);
        }
        catch (Exception ex)
        {
            return new AddPersonResult(false, ex.Message);
        }
    }

    public UpdatePersonResult UpdatePerson(Person person)
    {
        try
        {
            using var db = GetDatabaseContext();
            var persons = db.GetCollectionWithPlural<Person>();
            var success = persons.Update(person);

            EnsureIndexes(persons);

            return new UpdatePersonResult(success, success ? "Person updated" : "Person update failed");
        }
        catch (Exception ex)
        {
            return new UpdatePersonResult(false, ex.Message);
        }
    }

    public bool DeletePerson(Person person)
    {
        try
        {
            using var db = GetDatabaseContext();
            var personCollection = db.GetCollectionWithPlural<Person>();
            return personCollection.Delete(person.Id);
        }
        catch (Exception)
        {
            return false;
        }
    }
}

internal static class LiteDbExtensions
{
    public static ILiteCollection<TSource> GetCollectionWithPlural<TSource>(this LiteDatabase db)
    {
        var pluralName = typeof(TSource).Name.Pluralize();
        return db.GetCollection<TSource>(pluralName ?? typeof(TSource).Name);
    }
}
