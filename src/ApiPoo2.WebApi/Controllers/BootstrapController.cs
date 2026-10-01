using ApiPoo2.Application.UseCases.Bootstrap;
using ApiPoo2.Domain.Personas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace ApiPoo2.WebApi.Controllers;

/// <summary>
///     Arranque del sistema: crea el primer administrador a partir del archivo de configuración
///     y devuelve sus credenciales para iniciar sesión. Solo funciona con la base vacía; una vez
///     que existe una persona, el caso de uso responde 403.
/// </summary>
[ApiController]
[Route("api/bootstrap")]
[AllowAnonymous]
public sealed class BootstrapController : ControllerBase
{
    private readonly BootstrapAdminUseCase _bootstrap;
    private readonly IConfiguration _configuration;

    public BootstrapController(BootstrapAdminUseCase bootstrap, IConfiguration configuration)
    {
        _bootstrap = bootstrap;
        _configuration = configuration;
    }

    [HttpPost("admin")]
    public async Task<ActionResult<BootstrapAdminOutputDto>> CrearPrimerAdministrador(
        CancellationToken cancellationToken)
    {
        var seccion = _configuration.GetSection(BootstrapOptions.SectionName);

        var input = new BootstrapAdminInputDto(
            ParsearTipoIdentificacion(seccion["TipoIdentificacion"]),
            Requerido(seccion, nameof(BootstrapOptions.NumeroIdentificacion)),
            Requerido(seccion, nameof(BootstrapOptions.Nombres)),
            Requerido(seccion, nameof(BootstrapOptions.Apellidos)),
            Requerido(seccion, nameof(BootstrapOptions.CorreoElectronico)));

        return StatusCode(
            StatusCodes.Status201Created,
            await _bootstrap.ExecuteAsync(input, cancellationToken));
    }

    private static string Requerido(IConfigurationSection seccion, string clave)
    {
        var valor = seccion[clave];

        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new InvalidOperationException(
                $"Falta la configuración Bootstrap__{clave} en el archivo .env.");
        }

        return valor;
    }

    private static TipoIdentificacion ParsearTipoIdentificacion(string? valor)
    {
        if (int.TryParse(valor, out var numero)
            && Enum.IsDefined(typeof(TipoIdentificacion), numero))
        {
            return (TipoIdentificacion)numero;
        }

        if (Enum.TryParse<TipoIdentificacion>(valor, ignoreCase: true, out var tipo))
        {
            return tipo;
        }

        throw new InvalidOperationException(
            "Bootstrap__TipoIdentificacion debe ser 1-4 o uno de: CedulaCiudadania, CedulaExtranjeria, TarjetaIdentidad, Nit.");
    }
}

/// <summary>Nombres de las claves Bootstrap__* del archivo de configuración.</summary>
public static class BootstrapOptions
{
    public const string SectionName = "Bootstrap";

    public const string TipoIdentificacion = nameof(TipoIdentificacion);

    public const string NumeroIdentificacion = nameof(NumeroIdentificacion);

    public const string Nombres = nameof(Nombres);

    public const string Apellidos = nameof(Apellidos);

    public const string CorreoElectronico = nameof(CorreoElectronico);
}
