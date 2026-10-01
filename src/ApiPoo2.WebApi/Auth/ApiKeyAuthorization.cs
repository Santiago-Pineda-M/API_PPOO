using System.Security.Claims;
using ApiPoo2.Application.IRepositories;
using ApiPoo2.Domain.Personas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace ApiPoo2.WebApi.Auth;

/// <summary>
///     El enunciado exige que los servicios de escritura requieran token <b>y</b> APIKey. El JWT sigue
///     siendo el único que autentica; esta policy agrega la segunda verificación encima.
/// </summary>
public sealed class ApiKeyRequirement : IAuthorizationRequirement
{
    public const string HeaderName = "X-Api-Key";

    public const string PolicyName = "ApiKey";
}

/// <summary>
///     Los <see cref="AuthorizationHandler{TRequirement}" /> se registran como singleton, así que no
///     pueden inyectar un repositorio scoped. Se resuelve dentro de un scope propio por request.
/// </summary>
public sealed class ApiKeyHandler : AuthorizationHandler<ApiKeyRequirement>
{
    private readonly IServiceScopeFactory _scopeFactory;

    public ApiKeyHandler(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ApiKeyRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            // El JWT no validó: deja que el flujo de autenticación lo maneje.
            return;
        }

        var httpContext = context.Resource as HttpContext;

        if (httpContext is null)
        {
            return;
        }

        if (!httpContext.Request.Headers.TryGetValue(ApiKeyRequirement.HeaderName, out var values))
        {
            context.Fail(new AuthorizationFailureReason(this, $"Falta el encabezado {ApiKeyRequirement.HeaderName}."));
            return;
        }

        var presented = values.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(presented))
        {
            context.Fail(new AuthorizationFailureReason(this, "La APIKey está vacía."));
            return;
        }

        ApiKey apiKey;

        try
        {
            apiKey = ApiKey.From(presented);
        }
        catch (Domain.Common.DomainValidationException)
        {
            context.Fail(new AuthorizationFailureReason(this, "La APIKey no es válida."));
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var user = await userRepository.GetByApiKeyAsync(apiKey, httpContext.RequestAborted);

        if (user is null)
        {
            context.Fail(new AuthorizationFailureReason(this, "La APIKey no es válida."));
            return;
        }

        var personaId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub");

        if (personaId is null
            || !Guid.TryParse(personaId, out var idPersona)
            || idPersona != user.IdPersona)
        {
            context.Fail(new AuthorizationFailureReason(
                this,
                "La APIKey no corresponde al usuario autenticado."));
            return;
        }

        context.Succeed(requirement);
    }
}
