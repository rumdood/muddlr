using System.ComponentModel.DataAnnotations;

namespace Muddlr.WebFinger;

public class WebFingerLink
{
    [Required]
    public string Relationship { get; set; }
    public string? Type { get; set; }
    public Uri? Href { get; set; }
    public Dictionary<string, string>? Titles { get; set; }
    public string? Template { get; set; }
}
