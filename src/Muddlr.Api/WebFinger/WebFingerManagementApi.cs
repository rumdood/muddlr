using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Muddlr.WebFinger;

namespace Muddlr.Api;

internal static class WebFingerManagementApi
{
    public static RouteGroupBuilder MapWebFingerManagementApi(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/webfinger");
        group.WithTags("WebFingerMgmt");

        group.MapPost("/", async ([FromBody] WebFingerUpdateRequest updateRequest, IWebFingerService webFingerService) =>
        {
            var addResult = await webFingerService.AddWebFingerRecord(updateRequest);

            return addResult is not null
                ? Results.Created(
                    $"/.well-known/webfinger/?resource={addResult.Subject}", 
                    addResult!)
                : Results.BadRequest("Failed to create webfinger record");
        });

        return group;
    }
}