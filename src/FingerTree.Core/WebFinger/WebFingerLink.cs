using Newtonsoft.Json;

namespace FingerTree.WebFinger;

public class WebFingerLink
{
    [JsonProperty("rel")]
    public required string Relationship { get; set; }

    [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
    public string? Type { get; set; }

    [JsonProperty("href", NullValueHandling = NullValueHandling.Ignore)]
    public Uri? Href { get; set; }

    [JsonProperty("titles", NullValueHandling = NullValueHandling.Ignore)]
    public Dictionary<string, string>? Titles { get; set; }

    [JsonProperty("template", NullValueHandling = NullValueHandling.Ignore)]
    public string? Template { get; set; }
}