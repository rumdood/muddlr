﻿using Muddlr.Persons;
using Muddlr.WebFinger;

namespace Muddlr.Api;

internal record UpsertPersonDto(string Name, string Email, string[] Locators, string FediverseHandle, string FediverseServer, string Id = "")
{
    public Person ToPerson()
    {
        return new Person
        {
            Id = string.IsNullOrEmpty(Id) ? default : IdHasher.Instance.DecodeSingleLong(Id),
            Name = Name,
            Email = Email,
            Locators = new HashSet<string>(Locators.Select(loc =>
                !loc.StartsWith("acct:", StringComparison.OrdinalIgnoreCase) ? $"acct:{loc}" : loc)),
            FediverseHandle = FediverseHandle,
            FediverseServer = FediverseServer,
            Links = GenerateFediverseLinks(),
            Aliases = GenerateAliases()
        };
    }

    private HashSet<Uri> GenerateAliases()
    {
        return new HashSet<Uri>
        {
            new Uri($"https://{FediverseServer}/@{FediverseHandle}"),
            new Uri($"https://{FediverseServer}/users/{FediverseHandle}")
        };
    }

    private List<WebFingerLink> GenerateFediverseLinks()
    {
        return new List<WebFingerLink>
        {
            new WebFingerLink
            {
                Relationship = Relationships.WebFingerProfile,
                Type = LinkTypes.Text.Html,
                Href = new Uri($"https://{FediverseServer}/@{FediverseHandle}")
            },
            new WebFingerLink
            {
                Relationship = Relationships.Self,
                Type = LinkTypes.Application.ActivityJson,
                Href = new Uri($"https://{FediverseServer}/users/{FediverseHandle}")
            },
            new WebFingerLink
            {
                Relationship = Relationships.OStatusSubscribe,
                Template = $"https://{FediverseServer}/authorize_interaction?uri={{uri}}"
            },
        };
    }
}