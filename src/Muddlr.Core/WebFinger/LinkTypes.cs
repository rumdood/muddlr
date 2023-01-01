namespace Muddlr.WebFinger;

public static class LinkTypes
{
    public static class Text
    {
        private const string LiteralText = "text";
        
        public const string Html = $"{LiteralText}/html";
        public const string Json = $"{LiteralText}/json";
    }

    public static class Application
    {
        private const string LiteralApplication = "application";

        public const string ActivityJson = $"{LiteralApplication}/activity+json";
    }
}