using System.Reflection;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Muddlr.Api.HealthStatus;
using Muddlr.WebFinger;

namespace Muddlr.Api.Pages;

public class Index : PageModel
{
    private MuddlrStatus _status;
    public string ApiVersion => _status.ApiVersion;
    public string CoreVersion => _status.CoreVersion;
    public string ServerStatus => _status.Status.ToString();
    public void OnGet()
    {
        var apiAssembly = Assembly.GetExecutingAssembly().GetName();
        var apiVersion = apiAssembly.Version is not null
            ? apiAssembly.Version.ToString()
            : "UNK";

        var coreAssembly = typeof(WebFingerRecord).Assembly.GetName();
        var coreVersion = coreAssembly.Version is not null
            ? coreAssembly.Version.ToString()
            : "UNK";

        _status = new MuddlrStatus {ApiVersion = apiVersion, CoreVersion = coreVersion, Status = HealthStatus.HealthStatus.Ok};
    }
}