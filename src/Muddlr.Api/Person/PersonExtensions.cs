using Muddlr.Persons;

namespace Muddlr.Api;

internal static class PersonExtensions
{
    public static Person WithFilteredLinks(this Person person, params string[] linkFilters)
    {
        if (person is { Links: var links } && linkFilters.Any())
        {
            return new Person
            {
                Id = person.Id,
                Name = person.Name,
                Locators = person.Locators,
                Aliases = person.Aliases,
                FediverseHandle = person.FediverseHandle,
                FediverseServer = person.FediverseServer,
                Links = links
                    .EmptyIfNull()
                    .Where(link => linkFilters.Contains(link.Relationship))
                    .ToList()
            };
        }

        return person;
    }

    public static Person WithId(this Person person, long id)
    {
        person.Id = id;
        return person;
    }
}