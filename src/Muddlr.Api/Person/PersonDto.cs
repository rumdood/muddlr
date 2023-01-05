using Muddlr.Persons;
using Muddlr.WebFinger;

namespace Muddlr.Api;

internal record PersonDto(string Id, string Name, HashSet<string> Locators, HashSet<Uri>? Aliases, List<WebFingerLink>? Links, string FediverseServer, string FediverseHandle) : IPerson
{
    public static PersonDto FromPerson(Person person)
        => new
        (
            IdHasher.Instance.EncodeLong(person.Id),
            person.Name,
            person.Locators,
            person.Aliases,
            person.Links,
            person.FediverseServer,
            person.FediverseHandle
        );
}