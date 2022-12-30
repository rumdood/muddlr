using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace Muddlr.WebFinger;

public class WebFingerResponse
{
    [Required]
    [JsonProperty("subject")]
    public string Subject { get; set; }
    [JsonProperty("aliases", NullValueHandling = NullValueHandling.Ignore)]
    public Uri[]? Aliases { get; set; } = Array.Empty<Uri>();
    [JsonProperty("links", NullValueHandling = NullValueHandling.Ignore)]
    public WebFingerLink[]? Links { get; set; } = Array.Empty<WebFingerLink>();
    [JsonProperty("properties", NullValueHandling = NullValueHandling.Ignore)]
    public Dictionary<Uri, string>? Properties { get; set; }
}
