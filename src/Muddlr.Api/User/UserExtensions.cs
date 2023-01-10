using Muddlr.Users;

namespace Muddlr.Api;

internal static class PersonExtensions
{
    public static User WithFilteredLinks(this User user, params string[] linkFilters)
    {
        if (user is { Links: var links } && linkFilters.Any())
        {
            return new User
            {
                Id = user.Id,
                Name = user.Name,
                Locators = user.Locators,
                Aliases = user.Aliases,
                FediverseAccount = user.FediverseAccount,
                Links = links
                    .EmptyIfNull()
                    .Where(link => linkFilters.Contains(link.Relationship))
                    .ToList()
            };
        }

        return user;
    }

    public static User WithId(this User user, long id)
    {
        user.Id = id;
        return user;
    }
}