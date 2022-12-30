using Muddlr.Persons;
using Muddlr.WebFinger;
using Microsoft.AspNetCore.Mvc;

namespace Muddlr.Api;

internal static class PersonApi
{
    public static RouteGroupBuilder MapPersonApi(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/person");
        group.WithTags("Person");

        group.MapGet("/{id:long}", ([FromRoute] long id, IPersonRepository personRepo) =>
        {
            var person = personRepo.GetPerson(new PersonFilter {Id = id});

            return person is not null ? Results.Ok(person) : Results.NotFound();
        });

        group.MapGet("/", (IPersonRepository personRepo) =>
        {
            var people = personRepo.GetAllPersons();

            return Results.Ok(people);
        });
        
        group.MapPost("/new", ([FromBody] PersonDto personDto, IPersonRepository personRepo) =>
        {
            var addResult = personRepo.AddPerson(personDto.ToPerson());

            return addResult.Success 
                ? Results.Created($"/api/person/{addResult.user!.Id}", addResult.user) 
                : Results.BadRequest(addResult.Message);
        });
        
        group.MapPatch("/{id}", ([FromRoute] long id, [FromBody] PersonDto personDto, IPersonRepository personRepo) =>
        {
            var updateResult = personRepo.UpdatePerson(personDto.ToPerson().WithId(id));

            return updateResult.Success 
                ? Results.Accepted() 
                : Results.BadRequest(updateResult.Message);
        });

        return group;
    }
}

internal record PersonDto(string Name, string Email, string[] Locators, string FediverseHandle, string FediverseServer)
{
    public Person ToPerson()
    {
        return new Person
        {
            Name = Name,
            Email = Email,
            Locators = new HashSet<string>(Locators.Select(loc => !loc.StartsWith("acct:", StringComparison.OrdinalIgnoreCase) ? $"acct:{loc}" : loc)),
            FediverseHandle = FediverseHandle,
            FediverseServer = FediverseServer,
            Links = GenerateFediverseLinks(),
            Aliases = GenerateAliases()
        };
    }

    private HashSet<Uri> GenerateAliases()
    {
        return new HashSet<Uri>
        {
            new Uri($"https://{FediverseServer}/@{FediverseHandle}"),
            new Uri($"https://{FediverseServer}/users/{FediverseHandle}")
        };
    }

    private List<WebFingerLink> GenerateFediverseLinks()
    {
        return new List<WebFingerLink>
        {
            new WebFingerLink 
            { 
                Relationship = "http://webfinger.net/rel/profile-page", 
                Type = "text/html", 
                Href = new Uri($"https://{FediverseServer}/@{FediverseHandle}") 
            },
            new WebFingerLink
            {
                Relationship = "self",
                Type = "application/activity+json",
                Href = new Uri($"https://{FediverseServer}/users/{FediverseHandle}")
            },
            new WebFingerLink
            {
                Relationship = "http://ostatus.org/schema/1.0/subscribe",
                Template = $"https://{FediverseServer}/authorize_interaction?uri={{uri}}"
            },
        };
    }
}
