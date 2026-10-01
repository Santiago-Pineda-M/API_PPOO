using ApiPoo2.Application.UseCases.Auth;
using ApiPoo2.Domain.Personas;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Conductores;

public sealed record ChangeConductorEstadoInputDto(Guid PersonaId, Guid VehiculoId, EstadoConductor Estado);

public sealed class ChangeConductorEstadoUseCase : BaseUseCase<ChangeConductorEstadoInputDto, OperationResult>
{
    private readonly IConductorVehiculoRepository _conductorRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public ChangeConductorEstadoUseCase(
        IEnumerable<IValidator<ChangeConductorEstadoInputDto>> validators,
        ILoggerFactory loggerFactory,
        IConductorVehiculoRepository conductorRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
        : base(validators, loggerFactory)
    {
        _conductorRepository = conductorRepository;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    protected override async Task<OperationResult> ExecuteCoreAsync(
        ChangeConductorEstadoInputDto request,
        CancellationToken cancellationToken)
    {
        var relacion = await _conductorRepository
            .GetByPersonaAndVehiculoAsync(request.PersonaId, request.VehiculoId, cancellationToken)
            ?? throw new NotFoundException(
                "conductor.association.not_found",
                "El conductor no está asociado a ese vehículo.");

        relacion.CambiarEstado(request.Estado, _dateTimeProvider.UtcNow);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return OperationResult.Success();
    }
}
