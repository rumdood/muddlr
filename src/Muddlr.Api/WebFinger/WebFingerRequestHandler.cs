using Muddlr.WebFinger;

namespace Muddlr.Api;

public class WebFingerRequestHandler
{
    private readonly IWebFingerService _webFingerService;

    public WebFingerRequestHandler(IWebFingerService webFingerService)
    {
        _webFingerService = webFingerService;
    }

    public async Task<(WebFingerResult Status, WebFingerRecord? Response)> ProcessWebFingerRequest(WebFingerRequest request)
    {
        var webFinger = await _webFingerService.GetWebFingerRecord(request.Resource);
        var status = webFinger is not null ? WebFingerResult.Success : WebFingerResult.NotFound;

        return (status, webFinger);
    }
}
