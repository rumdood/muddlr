using Muddlr.Users;
using Muddlr.WebFinger;

namespace Muddlr.Api;

public class WebFingerRequestHandler
{
    private readonly IUserRepository _userRepository;

    public WebFingerRequestHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public (WebFingerResult Status, WebFingerResponse? Response) ProcessWebFingerRequest(WebFingerRequest request)
    {
        var user = _userRepository.GetUser(request.ToPersonFilter());

        return user is
            { FediverseAccount: var fedAccount, Aliases: var aliases, Links: var links}
            ? (WebFingerResult.Success, new WebFingerResponse
            {
                Subject = $"acct:{fedAccount.Username}@{fedAccount.Username}",
                Aliases = aliases?.ToArray(),
                Links = links?.ToArray()
            })
            : (WebFingerResult.NotFound, null);
    }
}

internal static class WebFingerRequestExtensions
{
    public static UserFilter ToPersonFilter(this WebFingerRequest request)
    {
        return new UserFilter
        {
            Locator = request.Resource,
            Relationships = request.Relationships,
        };
    }
}
