using Muddlr.WebFinger;
using Muddlr.Fediverse;

namespace Muddlr.Users;

public interface IUser
{
    string Name { get; }
    HashSet<string> Locators { get; }
    HashSet<Uri>? Aliases { get; }
    List<WebFingerLink>? Links { get; }
    FediverseAccount FediverseAccount { get; }
}

public class User : IUser
{
    public long Id { get; set; }
    public string Name { get; set; }
    public HashSet<string> Locators { get; set; } = new();
    public HashSet<Uri>? Aliases { get; set; }
    public List<WebFingerLink>? Links { get; set; } = new();
    public FediverseAccount FediverseAccount { get; set; }
}
