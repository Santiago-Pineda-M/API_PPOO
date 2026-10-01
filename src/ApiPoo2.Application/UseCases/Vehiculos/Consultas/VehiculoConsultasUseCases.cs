using ApiPoo2.Domain.Documentos;
using ApiPoo2.Domain.Vehiculos;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Vehiculos.Consultas;

public sealed record VehiculoResumenOutputDto(
    Guid Id,
    string Placa,
    TipoVehiculo TipoVehiculo,
    TipoServicio TipoServicio);

public sealed record GetVehiculosByTipoInputDto(TipoVehiculo TipoVehiculo);

public sealed class GetVehiculosByTipoUseCase
    : BaseUseCase<GetVehiculosByTipoInputDto, IReadOnlyList<VehiculoResumenOutputDto>>
{
    private readonly IVehiculoRepository _vehiculoRepository;

    public GetVehiculosByTipoUseCase(
        IEnumerable<IValidator<GetVehiculosByTipoInputDto>> validators,
        ILoggerFactory loggerFactory,
        IVehiculoRepository vehiculoRepository)
        : base(validators, loggerFactory)
    {
        _vehiculoRepository = vehiculoRepository;
    }

    protected override async Task<IReadOnlyList<VehiculoResumenOutputDto>> ExecuteCoreAsync(
        GetVehiculosByTipoInputDto request,
        CancellationToken cancellationToken)
        => (await _vehiculoRepository.GetByTipoAsync(request.TipoVehiculo, cancellationToken))
            .Select(v => new VehiculoResumenOutputDto(v.Id, v.Placa.Value, v.TipoVehiculo, v.TipoServicio))
            .ToList();
}

public sealed record GetVehiculosByDocumentoInputDto(string Codigo);

public sealed class GetVehiculosByDocumentoUseCase
    : BaseUseCase<GetVehiculosByDocumentoInputDto, IReadOnlyList<VehiculoResumenOutputDto>>
{
    private readonly IDocumentoRepository _documentoRepository;

    public GetVehiculosByDocumentoUseCase(
        IEnumerable<IValidator<GetVehiculosByDocumentoInputDto>> validators,
        ILoggerFactory loggerFactory,
        IDocumentoRepository documentoRepository)
        : base(validators, loggerFactory)
    {
        _documentoRepository = documentoRepository;
    }

    protected override async Task<IReadOnlyList<VehiculoResumenOutputDto>> ExecuteCoreAsync(
        GetVehiculosByDocumentoInputDto request,
        CancellationToken cancellationToken)
        => (await _documentoRepository.GetConDocumentoAsync(DocumentoCodigo.From(request.Codigo), cancellationToken))
            .Select(v => new VehiculoResumenOutputDto(v.Id, v.Placa.Value, v.TipoVehiculo, v.TipoServicio))
            .ToList();
}

public sealed record GetVehiculosByEstadoDocumentoInputDto(EstadoDocumento Estado);

public sealed class GetVehiculosByEstadoDocumentoUseCase
    : BaseUseCase<GetVehiculosByEstadoDocumentoInputDto, IReadOnlyList<VehiculoResumenOutputDto>>
{
    private readonly IVehiculoRepository _vehiculoRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetVehiculosByEstadoDocumentoUseCase(
        IEnumerable<IValidator<GetVehiculosByEstadoDocumentoInputDto>> validators,
        ILoggerFactory loggerFactory,
        IVehiculoRepository vehiculoRepository,
        IDateTimeProvider dateTimeProvider)
        : base(validators, loggerFactory)
    {
        _vehiculoRepository = vehiculoRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    protected override async Task<IReadOnlyList<VehiculoResumenOutputDto>> ExecuteCoreAsync(
        GetVehiculosByEstadoDocumentoInputDto request,
        CancellationToken cancellationToken)
    {
        var ahora = _dateTimeProvider.UtcNow;

        return (await _vehiculoRepository.GetAllWithDetailsAsync(cancellationToken))
            .Where(v => v.GetDocumentos().Any(d => d.EstadoActual(ahora) == request.Estado))
            .Select(v => new VehiculoResumenOutputDto(v.Id, v.Placa.Value, v.TipoVehiculo, v.TipoServicio))
            .ToList();
    }
}
