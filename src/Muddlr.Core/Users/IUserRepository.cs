namespace Muddlr.Users;

public interface IUserRepository
{
    User? GetUser(UserFilter filter);
    IEnumerable<User> GetAllUsers();
    AddUserResult AddUser(User user);
    UpdateUserResult UpdateUser(User user);
    bool DeleteUser(User user);
}

public sealed record AddUserResult(bool Success, string Message, User? AddedUser = null);
public sealed record UpdateUserResult(bool Success, string Message);

public class UserFilter
{
    public long Id { get; set; }
    public string? Locator { get; set; }
    public string[] Relationships { get; set; } = Array.Empty<string>();
}
