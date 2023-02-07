using Microsoft.AspNetCore.Http.HttpResults;
using Muddlr.WebFinger;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Muddlr.Api;

internal static class WebFingerApi
{
    public static RouteGroupBuilder MapWebFingerApi(this IEndpointRouteBuilder routes)
    {
        // var group = routes.MapGroup("/.well-known/webfinger/");
        var group = routes.MapGroup("/");
        group.WithTags("WebFinger");

        group.MapGet("/.well-known/webfinger/", async ([FromQuery, BindRequired] string resource, [FromQuery] string[] rel, WebFingerRequestHandler handler) =>
        {
            var request = new WebFingerRequest {Resource = resource, Relationships = rel};
            var (status, response) = await handler.ProcessWebFingerRequest(request);

            return status == WebFingerResult.Success
                ? Results.Ok(response)
                : Results.NotFound("Cannot find the requested resource");
        }).RequireCors("Everybody");
        group.MapGet("/@{locator}", async (string locator, HttpContext context, MuddlrApiConfig config, IWebFingerService fingerService) =>
        {
            var hostString = string.IsNullOrEmpty(config.ForDomain)
                ? context.Request.Host.Host
                : config.ForDomain;
            
            var fullLocator = $"acct:{locator}@{hostString}";
            var record = await fingerService.GetWebFingerRecord(fullLocator, Relationship.WebFingerProfile);

            if (record is not {Links: {Count: > 0}})
            {
                return Results.NotFound();
            }
            
            var profile = record.Links.Single().Href?.ToString();

            return !string.IsNullOrEmpty(profile) 
                ? Results.Redirect(profile, false, false) 
                : Results.NotFound();
        });

        return group;
    }
}
