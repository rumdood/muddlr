namespace Muddlr.WebFinger;

public class WebFingerRecord
{
    public string Subject { get; set; }
    public HashSet<Uri>? Aliases { get; set; }
    public List<WebFingerLink>? Links { get; set; }
    public Dictionary<Uri, string>? Properties { get; set; }
}
