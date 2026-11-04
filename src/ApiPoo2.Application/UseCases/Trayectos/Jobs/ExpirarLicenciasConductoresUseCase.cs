using ApiPoo2.Domain.Personas;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Trayectos.Jobs;

/// <summary>
///     Tarea programada: cuando la licencia de un conductor vence, todas sus asociaciones a
///     vehículos pasan a estado RO y se notifica al conductor (implementación por log).
/// </summary>
public sealed class ExpirarLicenciasConductoresUseCase : BaseUseCase<SinParametros, int>
{
    private readonly IPersonaRepository _personaRepository;
    private readonly IEmailNotifier _emailNotifier;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public ExpirarLicenciasConductoresUseCase(
        IEnumerable<FluentValidation.IValidator<SinParametros>> validators,
        ILoggerFactory loggerFactory,
        IPersonaRepository personaRepository,
        IEmailNotifier emailNotifier,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
        : base(validators, loggerFactory)
    {
        _personaRepository = personaRepository;
        _emailNotifier = emailNotifier;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    protected override async Task<int> ExecuteCoreAsync(
        SinParametros request,
        CancellationToken cancellationToken)
    {
        var ahora = _dateTimeProvider.UtcNow;
        var personas = await _personaRepository.GetConLicenciaVencidaAsync(ahora, cancellationToken);
        var restringidos = 0;

        foreach (var persona in personas)
        {
            if (!persona.TieneLicencia() || persona.LicenciaVigente(ahora))
            {
                continue;
            }

            var restringidasAhora = 0;

            foreach (var asociacion in persona.ConductoresVehiculosAssociated)
            {
                if (!asociacion.PuedeOperar())
                {
                    continue;
                }

                asociacion.Restringir(ahora);
                restringidasAhora++;
            }

            if (restringidasAhora == 0)
            {
                continue;
            }

            restringidos += restringidasAhora;
            await _emailNotifier.NotificarRestriccionAsync(
                persona.CorreoElectronico.Value,
                persona.Usuario?.Login.Value ?? persona.NumeroIdentificacion.Value,
                cancellationToken);
        }

        if (restringidos > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return restringidos;
    }
}
