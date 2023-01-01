using Muddlr.Persons;
using Muddlr.WebFinger;

namespace Muddlr.Api;

public class WebFingerRequestHandler
{
    private readonly IPersonRepository _personRepository;

    public WebFingerRequestHandler(IPersonRepository personRepository)
    {
        _personRepository = personRepository;
    }

    public (WebFingerResult Status, WebFingerResponse? Response) ProcessWebFingerRequest(WebFingerRequest request)
    {
        var user = _personRepository.GetPerson(request.ToPersonFilter());

        return user is
            {FediverseHandle: var handle, FediverseServer: var server, Aliases: var aliases, Links: var links}
            ? (WebFingerResult.Success, new WebFingerResponse
            {
                Subject = $"acct:{handle}@{server}",
                Aliases = aliases?.ToArray(),
                Links = links?.ToArray()
            })
            : (WebFingerResult.NotFound, null);
    }
}

internal static class WebFingerRequestExtensions
{
    public static PersonFilter ToPersonFilter(this WebFingerRequest request)
    {
        return new PersonFilter
        {
            Locator = request.Resource,
            Relationships = request.Relationships,
        };
    }
}
