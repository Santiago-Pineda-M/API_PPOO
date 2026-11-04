using ApiPoo2.Domain.Rutas;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Rutas.Update;

public sealed record UpdateRutaInputDto(
    string CodigoRuta,
    Guid PersonaId,
    Guid VehiculoId);

public sealed class UpdateRutaValidator : AbstractValidator<UpdateRutaInputDto>
{
    public UpdateRutaValidator()
    {
        RuleFor(x => x.CodigoRuta)
            .NotEmpty().WithMessage("El código de ruta es obligatorio.")
            .MaximumLength(32).WithMessage("El código de ruta no puede superar los 32 caracteres.");

        RuleFor(x => x.PersonaId)
            .NotEmpty().WithMessage("El conductor es obligatorio.");

        RuleFor(x => x.VehiculoId)
            .NotEmpty().WithMessage("El vehículo es obligatorio.");
    }
}

public sealed class UpdateRutaUseCase : BaseUseCase<UpdateRutaInputDto, RutaConsultaOutputDto>
{
    private readonly IRutaRepository _rutaRepository;
    private readonly IPersonaRepository _personaRepository;
    private readonly IVehiculoRepository _vehiculoRepository;
    private readonly IConductorVehiculoRepository _conductorRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateRutaUseCase(
        IEnumerable<IValidator<UpdateRutaInputDto>> validators,
        ILoggerFactory loggerFactory,
        IRutaRepository rutaRepository,
        IPersonaRepository personaRepository,
        IVehiculoRepository vehiculoRepository,
        IConductorVehiculoRepository conductorRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
        : base(validators, loggerFactory)
    {
        _rutaRepository = rutaRepository;
        _personaRepository = personaRepository;
        _vehiculoRepository = vehiculoRepository;
        _conductorRepository = conductorRepository;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    protected override async Task<RutaConsultaOutputDto> ExecuteCoreAsync(
        UpdateRutaInputDto request,
        CancellationToken cancellationToken)
    {
        var ahora = _dateTimeProvider.UtcNow;
        var codigo = CodigoRuta.From(request.CodigoRuta);

        var ruta = await _rutaRepository.GetByCodigoAsync(codigo, cancellationToken)
            ?? throw new NotFoundException("ruta.not_found", "La ruta no existe.");

        var persona = await _personaRepository.GetByIdAsync(request.PersonaId, cancellationToken)
            ?? throw new NotFoundException("person.not_found", "La persona no existe.");

        var vehiculo = await _vehiculoRepository.GetByIdAsync(request.VehiculoId, cancellationToken)
            ?? throw new NotFoundException("vehicle.not_found", "El vehículo no existe.");

        var asociacion = await _conductorRepository.GetByPersonaAndVehiculoAsync(
            request.PersonaId,
            request.VehiculoId,
            cancellationToken);

        RutaOperacionValidator.Validar(persona, vehiculo, asociacion, ahora);

        ruta.Reasignar(persona.Id, vehiculo.Id, ahora);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return RutaConsultaOutputDto.From(ruta, persona, vehiculo, ahora);
    }
}
