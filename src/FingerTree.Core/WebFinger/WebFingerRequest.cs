using Newtonsoft.Json;

namespace FingerTree.WebFinger;

public class WebFingerRequest
{
    [JsonProperty("resource")]
    public required string Resource { get; set; }

    [JsonProperty("rel")] 
    public string[] Relationships { get; set; } = Array.Empty<string>();
}