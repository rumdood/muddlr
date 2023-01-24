using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Muddlr.WebFinger;

namespace Muddlr.Api;

internal static class WebFingerManagementApi
{
    public static RouteGroupBuilder MapWebFingerManagementApi(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/webfinger");
        group.WithTags("WebFingerMgmt");

        group.MapPost("/", [Authorize] async ([FromBody] WebFingerUpdateRequest updateRequest, IWebFingerService webFingerService) =>
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