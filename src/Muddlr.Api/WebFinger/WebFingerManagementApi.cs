using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Muddlr.WebFinger;

namespace Muddlr.Api;

internal static class WebFingerManagementApi
{
    public static RouteGroupBuilder MapWebFingerManagementApi(this IEndpointRouteBuilder routes)
    {
        const string resourceUrl = "/.well-known/webfinger/?resource=";
        
        var group = routes.MapGroup("/api/webfinger");
        group.WithTags("WebFingerMgmt");

        group.MapPost("/", [Authorize] async ([FromBody] WebFingerUpdateRequest updateRequest, IWebFingerService webFingerService) =>
        {
            var addResult = await webFingerService.AddWebFingerRecord(updateRequest);

            return addResult is not null
                ? Results.Created($"{resourceUrl}{addResult.Subject}", addResult!)
                : Results.BadRequest("Failed to create webfinger record");
        });

        group.MapPatch("/", [Authorize]
            async ([FromBody] WebFingerUpdateRequest updateRequest, IWebFingerService webFingerService) =>
            {
                var updateResult = await webFingerService.UpdateWebFingerRecord(updateRequest);

                return updateResult is not null
                    ? Results.Accepted($"{resourceUrl}{updateResult.Subject}", updateResult!)
                    : Results.BadRequest("Failed to update webfinger record");
            });
        
        group.MapPut("/", [Authorize]
            async ([FromBody] WebFingerUpdateRequest updateRequest, IWebFingerService webFingerService) =>
            {
                var isExisting = false;
                foreach (var locator in updateRequest.Locators)
                {
                    var existing = await webFingerService.GetWebFingerRecord(locator);

                    if (existing is null)
                    {
                        continue;
                    }
                    
                    isExisting = true;
                    break;
                }

                var result = isExisting
                    ? await webFingerService.AddWebFingerRecord(updateRequest)
                    : await webFingerService.UpdateWebFingerRecord(updateRequest);

                return result is { Subject: { } }
                    ? isExisting
                        ? Results.Accepted($"{resourceUrl}{result.Subject}", result!)
                        : Results.Created($"{resourceUrl}{result.Subject}", result!)
                    : Results.BadRequest("Failed to create or update webfinger record");
            });

        return group;
    }
}