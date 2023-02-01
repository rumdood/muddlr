using Muddlr.WebFinger;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Muddlr.Api;

internal static class WebFingerApi
{
    public static RouteGroupBuilder MapWebFingerApi(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/.well-known/webfinger/");
        group.WithTags("WebFinger");

        group.MapGet("/", async ([FromQuery, BindRequired] string resource, [FromQuery] string[] rel, WebFingerRequestHandler handler) =>
        {
            var request = new WebFingerRequest {Resource = resource, Relationships = rel};
            var (status, response) = await handler.ProcessWebFingerRequest(request);

            return status == WebFingerResult.Success
                ? Results.Ok(response)
                : Results.NotFound("Cannot find the requested resource");
        }).RequireCors("Everybody");

        return group;
    }
}
