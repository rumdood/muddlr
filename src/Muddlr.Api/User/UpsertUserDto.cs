using Muddlr.Fediverse;
using Muddlr.Users;
using Muddlr.WebFinger;

namespace Muddlr.Api;

internal record UpsertUserDto(string Name, string[] Locators, string FediverseUsername, string FediverseServer, string Id = "")
{
    public User ToUser()
    {
        if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(FediverseUsername) ||
            string.IsNullOrWhiteSpace(FediverseServer))
        {
            throw new InvalidOperationException("Mandatory fields not included");
        }
        
        return new User
        {
            Id = string.IsNullOrEmpty(Id) ? default : IdHasher.Instance.DecodeSingleLong(Id),
            Name = Name,
            Locators = new HashSet<string>(Locators.Select(loc =>
                !loc.StartsWith("acct:", StringComparison.OrdinalIgnoreCase) ? $"acct:{loc}" : loc)),
            FediverseAccount = new FediverseAccount() { Server = FediverseServer, Username = FediverseUsername },
            Links = GenerateFediverseLinks(),
            Aliases = GenerateAliases()
        };
    }

    private HashSet<Uri> GenerateAliases()
    {
        return new HashSet<Uri>
        {
            new Uri($"https://{FediverseServer}/@{FediverseUsername}"),
            new Uri($"https://{FediverseServer}/users/{FediverseUsername}")
        };
    }

    private List<WebFingerLink> GenerateFediverseLinks()
    {
        return new List<WebFingerLink>
        {
            new WebFingerLink
            {
                Relationship = Relationship.WebFingerProfile,
                Type = LinkType.TextHtml,
                Href = new Uri($"https://{FediverseServer}/@{FediverseUsername}")
            },
            new WebFingerLink
            {
                Relationship = Relationship.Self,
                Type = LinkType.ApplicationActivityJson,
                Href = new Uri($"https://{FediverseServer}/users/{FediverseUsername}")
            },
            new WebFingerLink
            {
                Relationship = Relationship.OStatusSubscribe,
                Template = $"https://{FediverseServer}/authorize_interaction?uri={{uri}}"
            },
        };
    }
}
