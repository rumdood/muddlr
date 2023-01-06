using System.Text.Json.Serialization;
using Ardalis.SmartEnum;
using Ardalis.SmartEnum.SystemTextJson;

namespace Muddlr.WebFinger;

public class Relationship : SmartEnum<Relationship, string>
{
    public static readonly Relationship None = new(nameof(None), "");
    public static readonly Relationship Self = new(name: nameof(Self), value: "self");

    public static readonly Relationship WebFingerProfile =
        new(name: nameof(WebFingerProfile), value: "http://webfinger.net/rel/profile-page");

    public static readonly Relationship OStatusSubscribe =
        new(name: nameof(OStatusSubscribe), value: "http://ostatus.org/schema/1.0/subscribe");

    private Relationship(string name, string value) : base(name, value) { }
}
