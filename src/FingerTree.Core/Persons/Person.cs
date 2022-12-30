using FingerTree.WebFinger;

namespace FingerTree.Persons;

public class Person
{
    public long Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public HashSet<string> Locators { get; set; } = new();
    public HashSet<Uri>? Aliases { get; set; }
    public List<WebFingerLink>? Links { get; set; } = new();
    public required string FediverseServer { get; set; }
    public required string FediverseHandle { get; set;}
}
