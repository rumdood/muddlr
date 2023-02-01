namespace Muddlr.WebFinger;

public interface IWebFingerService
{
    Task<WebFingerRecord> AddWebFingerRecord(WebFingerUpdateRequest request);
    Task<WebFingerRecord> UpdateWebFingerRecord(WebFingerUpdateRequest request);
    Task<bool> AddLocator(string locator, FediverseAccount account);
    Task<bool> RemoveLocator(string locator, FediverseAccount account);
    Task<bool> DeleteWebFingerRecord(FediverseAccount account);
    Task<WebFingerRecord> AddLinks(FediverseAccount account, IEnumerable<WebFingerLink> links);
    Task<WebFingerRecord> RemoveLinks(FediverseAccount account, IEnumerable<WebFingerLink> links);
    Task<WebFingerRecord?> GetWebFingerRecord(string locator, params string[] relationships);
}

public class WebFingerUpdateRequest
{
    public string[] Locators { get; init; } = Array.Empty<string>();
    public FediverseAccount Account { get; init; }
    public WebFingerLink[]? Links { get; init; }

    public WebFingerUpdateRequest()
    {
    }
    
    public WebFingerUpdateRequest(string[] locators, FediverseAccount account, IEnumerable<WebFingerLink>? links = null)
    {
        Locators = locators;
        Account = account;
        if (links != null)
        {
            Links = links.ToArray();
        }
    }
    
    public WebFingerUpdateRequest(
        string[] locators,
        string fediverseServer,
        string accountName,
        IEnumerable<WebFingerLink> links)
        : this(
            locators,
            new FediverseAccount(fediverseServer, accountName),
            links
        ) { }
}
