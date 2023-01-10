namespace Muddlr.WebFinger;

public class WebFingerRequest
{
    public string Resource { get; set; }
    public string[] Relationships { get; set; } = Array.Empty<string>();
}
