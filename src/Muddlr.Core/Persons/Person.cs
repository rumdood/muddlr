using Muddlr.WebFinger;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace Muddlr.Persons;

public class Person
{
    [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
    public long Id { get; set; }

    [Required]
    [JsonProperty("name", Required = Required.DisallowNull)]
    public string Name { get; set; }
    [Required]
    [JsonProperty("email", Required = Required.DisallowNull)]
    public string Email { get; set; }
    [JsonProperty("locators", Required = Required.DisallowNull)]
    public HashSet<string> Locators { get; set; } = new();
    [JsonProperty("aliases", NullValueHandling = NullValueHandling.Ignore)]
    public HashSet<Uri>? Aliases { get; set; }
    [JsonProperty("links", NullValueHandling = NullValueHandling.Ignore)]
    public List<WebFingerLink>? Links { get; set; } = new();
    [Required]
    [JsonProperty("fediverseServer", Required = Required.DisallowNull)]
    public string FediverseServer { get; set; }
    [Required]
    [JsonProperty("fediversHandle", Required = Required.DisallowNull)]
    public string FediverseHandle { get; set;}
}
