using ApiPoo2.Application.DTOs;
using ApiPoo2.Application.UseCases.Auth.GetCurrentUser;
using ApiPoo2.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiPoo2.WebApi.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public sealed class UsersController : ControllerBase
{
    private readonly GetCurrentUserUseCase _getCurrentUser;

    public UsersController(GetCurrentUserUseCase getCurrentUser)
    {
        _getCurrentUser = getCurrentUser;
    }

    [HttpGet("me")]
    public async Task<ActionResult<CurrentUserDto>> GetCurrentUser(CancellationToken cancellationToken)
        => Ok(await _getCurrentUser.ExecuteAsync(new GetCurrentUserInputDto(User.GetUserId()), cancellationToken));
}