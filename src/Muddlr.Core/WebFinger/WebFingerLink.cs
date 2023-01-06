using System.Text.Json.Serialization;
using Ardalis.SmartEnum.SystemTextJson;

namespace Muddlr.WebFinger;

public class WebFingerLink
{
    [JsonConverter(typeof(SmartEnumValueConverter<Relationship, string>))]
    public Relationship Relationship { get; set; }
    
    [JsonConverter(typeof(SmartEnumValueConverter<LinkType, string?>))]
    public LinkType? Type { get; set; }
    
    public Uri? Href { get; set; }
    public Dictionary<string, string>? Titles { get; set; }
    public string? Template { get; set; }
}
