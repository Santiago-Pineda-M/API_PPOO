using ApiPoo2.Application.Models;
using ApiPoo2.Application.CQRS;
using ApiPoo2.Application.CQRS.Auth.Queries.GetCurrentUser;
using ApiPoo2.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiPoo2.WebApi.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public sealed class UsersController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public UsersController(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetCurrentUser(CancellationToken cancellationToken)
    {
        var result = await _dispatcher.QueryAsync<GetCurrentUserQuery, UserDto>(
            new GetCurrentUserQuery(User.GetUserId()),
            cancellationToken);

        return Ok(result);
    }
}