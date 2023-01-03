using System.ComponentModel.DataAnnotations;

namespace Muddlr.WebFinger;

public class WebFingerResponse
{
    [Required]
    public string Subject { get; set; }
    public Uri[]? Aliases { get; set; } = Array.Empty<Uri>();
    public WebFingerLink[]? Links { get; set; } = Array.Empty<WebFingerLink>();
    public Dictionary<Uri, string>? Properties { get; set; }
}
