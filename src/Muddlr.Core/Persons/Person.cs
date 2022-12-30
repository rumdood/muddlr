using Muddlr.WebFinger;
using System.ComponentModel.DataAnnotations;

namespace Muddlr.Persons;

public class Person
{
    public long Id { get; set; }

    [Required]
    public string Name { get; set; }
    [Required]
    public string Email { get; set; }
    public HashSet<string> Locators { get; set; } = new();
    public HashSet<Uri>? Aliases { get; set; }
    public List<WebFingerLink>? Links { get; set; } = new();
    [Required]
    public string FediverseServer { get; set; }
    [Required]
    public string FediverseHandle { get; set;}
}
