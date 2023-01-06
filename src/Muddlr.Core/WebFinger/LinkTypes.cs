using Ardalis.SmartEnum;
using System.Text.Json.Serialization;
using Ardalis.SmartEnum.SystemTextJson;

namespace Muddlr.WebFinger;

public class LinkType: SmartEnum<LinkType, string?>
{
    public static LinkType None = new(name: nameof(None), value: null);
    public static LinkType TextHtml = new(name: nameof(TextHtml), value: "text/html");
    public static LinkType ApplicationActivityJson =
        new(name: nameof(ApplicationActivityJson), value: "application/activity+json");
    
    private LinkType(string name, string? value) : base(name, value ?? "") { }
}