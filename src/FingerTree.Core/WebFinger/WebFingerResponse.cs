using Newtonsoft.Json;

namespace FingerTree.WebFinger;

public class WebFingerResponse
{
    [JsonProperty("subject")]
    public required string Subject { get; set; }
    [JsonProperty("aliases", NullValueHandling = NullValueHandling.Ignore)]
    public Uri[]? Aliases { get; set; } = Array.Empty<Uri>();
    [JsonProperty("links", NullValueHandling = NullValueHandling.Ignore)]
    public WebFingerLink[]? Links { get; set; } = Array.Empty<WebFingerLink>();
    [JsonProperty("properties", NullValueHandling = NullValueHandling.Ignore)]
    public Dictionary<Uri, string>? Properties { get; set; }
}