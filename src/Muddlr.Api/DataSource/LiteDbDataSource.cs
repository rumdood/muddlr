using Muddlr.Users;
using Muddlr.WebFinger;
using Humanizer;
using LiteDB;

namespace Muddlr.Api;

internal class LiteDbDataSource: IUserRepository
{
    private readonly string _connectionString;

    public LiteDbDataSource(string connectionString)
    {
        BsonMapper.Global.Entity<Relationship>()
            .Ctor(x => Relationship.FromValue(x["Value"]));
        BsonMapper.Global.Entity<LinkType>()
            .Ctor(x => LinkType.FromValue(x["Value"]));
        _connectionString = connectionString;
    }

    private LiteDatabase GetDatabaseContext() => new(_connectionString);
    
    private static void EnsureIndexes(ILiteCollection<User> collection)
    {
        collection.EnsureIndex(u => u.Id);
        collection.EnsureIndex(u => u.Locators);
        collection.EnsureIndex("AliasesByRel", "LOWER($.Aliases[*].Relationship)");
    }

    public IEnumerable<User> GetAllUsers()
    {
        using var db = GetDatabaseContext();
        var userCollection = db.GetCollectionWithPlural<User>();
        return userCollection.Query().ToList();
    }

    public User? GetUser(UserFilter filter)
    {
        using var db = GetDatabaseContext();
        var userCollection = db.GetCollectionWithPlural<User>();
        var user = string.IsNullOrEmpty(filter.Locator)
            ? userCollection.Query()
                .Where(x => x.Id == filter.Id)
                .SingleOrDefault()
            : userCollection.Query()
                .Where(x => x.Locators.Contains(filter.Locator))
                .SingleOrDefault();

        return user is not null && filter.Relationships.Any() 
            ? user.WithFilteredLinks(filter.Relationships)
            : user;
    }

    public AddUserResult AddUser(User user)
    {
        try
        { 
            using var db = GetDatabaseContext();
            
            var userCollection = db.GetCollectionWithPlural<User>();
            var insertedId = userCollection.Insert(user);

            EnsureIndexes(userCollection);

            var inserted = userCollection.Query()
                .Where(x => x.Id == insertedId)
                .Single();

            return new AddUserResult(true, "User added", inserted);
        }
        catch (Exception ex)
        {
            return new AddUserResult(false, ex.Message);
        }
    }

    public UpdateUserResult UpdateUser(User user)
    {
        try
        {
            using var db = GetDatabaseContext();
            var users = db.GetCollectionWithPlural<User>();
            var success = users.Update(user);

            EnsureIndexes(users);

            return new UpdateUserResult(success, success ? "User updated" : "User update failed");
        }
        catch (Exception ex)
        {
            return new UpdateUserResult(false, ex.Message);
        }
    }

    public bool DeleteUser(User user)
    {
        try
        {
            using var db = GetDatabaseContext();
            var userCollection = db.GetCollectionWithPlural<User>();
            return userCollection.Delete(user.Id);
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
