using System.ComponentModel.DataAnnotations;

namespace Muddlr.WebFinger;

public class WebFingerRequest
{
    [Required]
    public string Resource { get; set; }
    public string[] Relationships { get; set; } = Array.Empty<string>();
}
