using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace Muddlr.WebFinger;

public class WebFingerLink
{
    [JsonProperty("rel")]
    [Required]
    public string Relationship { get; set; }

    [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
    public string? Type { get; set; }

    [JsonProperty("href", NullValueHandling = NullValueHandling.Ignore)]
    public Uri? Href { get; set; }

    [JsonProperty("titles", NullValueHandling = NullValueHandling.Ignore)]
    public Dictionary<string, string>? Titles { get; set; }

    [JsonProperty("template", NullValueHandling = NullValueHandling.Ignore)]
    public string? Template { get; set; }
}
