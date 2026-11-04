using FluentValidation;
using ApiPoo2.Domain.Rutas;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Rutas.Create;

public sealed class CreateRutaUseCase : BaseUseCase<CreateRutaInputDto, RutaConsultaOutputDto>
{
    private readonly IRutaRepository _rutaRepository;
    private readonly IPersonaRepository _personaRepository;
    private readonly IVehiculoRepository _vehiculoRepository;
    private readonly IConductorVehiculoRepository _conductorRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public CreateRutaUseCase(
        IEnumerable<IValidator<CreateRutaInputDto>> validators,
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
        CreateRutaInputDto request,
        CancellationToken cancellationToken)
    {
        var ahora = _dateTimeProvider.UtcNow;
        var codigo = CodigoRuta.From(request.CodigoRuta);

        if (await _rutaRepository.ExistsAsync(codigo, cancellationToken))
        {
            throw new ConflictException(
                "ruta.codigo.conflict",
                "Ya existe una ruta registrada con ese código.");
        }

        var persona = await _personaRepository.GetByIdAsync(request.PersonaId, cancellationToken)
            ?? throw new NotFoundException("person.not_found", "La persona no existe.");

        var vehiculo = await _vehiculoRepository.GetByIdAsync(request.VehiculoId, cancellationToken)
            ?? throw new NotFoundException("vehicle.not_found", "El vehículo no existe.");

        var asociacion = await _conductorRepository.GetByPersonaAndVehiculoAsync(
            request.PersonaId,
            request.VehiculoId,
            cancellationToken);

        RutaOperacionValidator.Validar(persona, vehiculo, asociacion, ahora);

        var ruta = Ruta.Registrar(
            codigo.Value,
            persona.Id,
            vehiculo.Id,
            request.LoginUsuario!,
            request.UbicacionInicial,
            request.Latitud,
            request.Longitud,
            ahora);

        _rutaRepository.Add(ruta);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return RutaConsultaOutputDto.From(ruta, persona, vehiculo, ahora);
    }
}
