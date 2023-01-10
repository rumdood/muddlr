using Muddlr.Fediverse;
using Muddlr.Users;
using Muddlr.WebFinger;

namespace Muddlr.Api;

internal record UserDto(string Id, string Name, HashSet<string> Locators, HashSet<Uri>? Aliases, List<WebFingerLink>? Links, FediverseAccount FediverseAccount) : IUser
{
    public static UserDto FromUser(User user)
        => new
        (
            IdHasher.Instance.EncodeLong(user.Id),
            user.Name,
            user.Locators,
            user.Aliases,
            user.Links,
            user.FediverseAccount
        );
}