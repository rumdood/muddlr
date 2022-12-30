using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace Muddlr.WebFinger;

public class WebFingerRequest
{
    [Required]
    [JsonProperty("resource")]
    public string Resource { get; set; }

    [JsonProperty("rel")] 
    public string[] Relationships { get; set; } = Array.Empty<string>();
}
