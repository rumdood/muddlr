using Microsoft.AspNetCore.Mvc;
using Muddlr.Persons;

namespace Muddlr.Api;

internal static class PersonApi
{
    public static RouteGroupBuilder MapPersonApi(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/person");
        group.WithTags("Person");
        
        group.MapGet("/", (IPersonRepository personRepo) =>
        {
            var people = personRepo.GetAllPersons().Select(PersonDto.FromPerson);
            return Results.Ok(people);
        });
        
        group.MapPost("/", ([FromBody] UpsertPersonDto personDto, IPersonRepository personRepo) =>
        {
            var addResult = personRepo.AddPerson(personDto.ToPerson());

            return addResult is {Success: true, AddedPerson: { Id: var id }}
                ? Results.Created(
                    $"/api/person/{IdHasher.Instance.EncodeLong(id)}", 
                    PersonDto.FromPerson(addResult.AddedPerson)) 
                : Results.BadRequest(addResult.Message);
        });

        group.MapGet("/{id}", ([FromRoute] string id, IPersonRepository personRepo) =>
        {
            var realId = IdHasher.Instance.DecodeSingleLong(id);
            var person = personRepo.GetPerson(new PersonFilter {Id = realId});

            return person is not null ? Results.Ok(PersonDto.FromPerson(person)) : Results.NotFound();
        });

        group.MapPut("/{id}", ([FromRoute] string id, [FromBody] UpsertPersonDto personDto, IPersonRepository personRepo) =>
        {
            var realId = IdHasher.Instance.DecodeSingleLong(id);
            var updateResult = personRepo.UpdatePerson(personDto.ToPerson().WithId(realId));

            return updateResult.Success 
                ? Results.Accepted() 
                : Results.BadRequest(updateResult.Message);
        });

        return group;
    }
}
