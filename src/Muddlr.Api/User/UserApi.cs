using Microsoft.AspNetCore.Mvc;
using Muddlr.Users;

namespace Muddlr.Api;

internal static class UserApi
{
    public static RouteGroupBuilder MapUserApi(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/user");
        group.WithTags("User");
        
        group.MapGet("/", (IUserRepository userRepo) =>
        {
            var people = userRepo.GetAllUsers().Select(UserDto.FromUser);
            return Results.Ok(people);
        });
        
        group.MapPost("/", ([FromBody] UpsertUserDto userDto, IUserRepository userRepo) =>
        {
            var addResult = userRepo.AddUser(userDto.ToUser());

            return addResult is {Success: true, AddedUser: { Id: var id }}
                ? Results.Created(
                    $"/api/user/{IdHasher.Instance.EncodeLong(id)}", 
                    UserDto.FromUser(addResult.AddedUser)) 
                : Results.BadRequest(addResult.Message);
        });

        group.MapGet("/{id}", ([FromRoute] string id, IUserRepository userRepo) =>
        {
            var realId = IdHasher.Instance.DecodeSingleLong(id);
            var user = userRepo.GetUser(new UserFilter {Id = realId});

            return user is not null ? Results.Ok(UserDto.FromUser(user)) : Results.NotFound();
        });

        group.MapPut("/{id}", ([FromRoute] string id, [FromBody] UpsertUserDto userDto, IUserRepository userRepo) =>
        {
            var realId = IdHasher.Instance.DecodeSingleLong(id);
            var updateResult = userRepo.UpdateUser(userDto.ToUser().WithId(realId));

            return updateResult.Success 
                ? Results.Accepted() 
                : Results.BadRequest(updateResult.Message);
        });

        return group;
    }
}
