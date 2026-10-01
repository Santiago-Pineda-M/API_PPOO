using ApiPoo2.Domain.Personas;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Conductores;

/// <summary>Asocia uno o varios vehículos que puede operar un conductor.</summary>
public sealed record AssociateVehiculosInputDto(Guid PersonaId, IReadOnlyList<Guid> VehiculoIds);

public sealed record ConductorVehiculoOutputDto(
    Guid PersonaId,
    Guid VehiculoId,
    DateTime FechaAsociacion,
    EstadoConductor Estado);

public sealed class AssociateVehiculosUseCase : BaseUseCase<AssociateVehiculosInputDto, IReadOnlyList<ConductorVehiculoOutputDto>>
{
    private readonly IPersonaRepository _personaRepository;
    private readonly IVehiculoRepository _vehiculoRepository;
    private readonly IConductorVehiculoRepository _conductorRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public AssociateVehiculosUseCase(
        IEnumerable<IValidator<AssociateVehiculosInputDto>> validators,
        ILoggerFactory loggerFactory,
        IPersonaRepository personaRepository,
        IVehiculoRepository vehiculoRepository,
        IConductorVehiculoRepository conductorRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
        : base(validators, loggerFactory)
    {
        _personaRepository = personaRepository;
        _vehiculoRepository = vehiculoRepository;
        _conductorRepository = conductorRepository;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    protected override async Task<IReadOnlyList<ConductorVehiculoOutputDto>> ExecuteCoreAsync(
        AssociateVehiculosInputDto request,
        CancellationToken cancellationToken)
    {
        // Invariante que un CHECK de SQL no puede expresar: solo personas CONDUCTOR se asocian.
        var persona = await _personaRepository.GetByIdAsync(request.PersonaId, cancellationToken)
            ?? throw new NotFoundException("person.not_found", "La persona no existe.");

        if (!persona.EsConductor())
        {
            throw new ForbiddenException(
                "conductor.persona.invalida",
                "Solo las personas de tipo CONDUCTOR pueden asociarse a vehículos.");
        }

        if (request.VehiculoIds.Count == 0)
        {
            throw new RequestValidationException(["Debe indicar al menos un vehículo."]);
        }

        var now = _dateTimeProvider.UtcNow;
        var resultados = new List<ConductorVehiculoOutputDto>();

        foreach (var vehiculoId in request.VehiculoIds.Distinct())
        {
            if (await _vehiculoRepository.GetByIdAsync(vehiculoId, cancellationToken) is null)
            {
                throw new NotFoundException("vehicle.not_found", $"El vehículo {vehiculoId} no existe.");
            }

            var relacion = ConductorVehiculo.Crear(persona.Id, vehiculoId, now);
            _conductorRepository.Add(relacion);

            resultados.Add(new ConductorVehiculoOutputDto(
                relacion.PersonaId,
                relacion.VehiculoId,
                relacion.FechaAsociacion,
                relacion.Estado));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return resultados;
    }
}
