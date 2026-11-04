using ApiPoo2.Domain.Trayectos;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Cargues;

public sealed class GetCargueUseCase : BaseUseCase<GetCargueInputDto, GetCargueOutputDto>
{
    private readonly ITrayectoTemporalRepository _trayectoRepository;

    public GetCargueUseCase(
        IEnumerable<FluentValidation.IValidator<GetCargueInputDto>> validators,
        ILoggerFactory loggerFactory,
        ITrayectoTemporalRepository trayectoRepository)
        : base(validators, loggerFactory)
    {
        _trayectoRepository = trayectoRepository;
    }

    protected override async Task<GetCargueOutputDto> ExecuteCoreAsync(
        GetCargueInputDto request,
        CancellationToken cancellationToken)
    {
        var filas = await _trayectoRepository.GetByIdCargueAsync(
            request.IdCargue,
            cancellationToken: cancellationToken);

        if (filas.Count == 0)
        {
            throw new NotFoundException("cargue.not_found", "El cargue no existe.");
        }

        var visibles = request.Estado is null
            ? filas
            : filas.Where(f => f.Estado == request.Estado.Value).ToList();

        return new GetCargueOutputDto(
            request.IdCargue,
            filas.Min(f => f.CreatedAtUtc),
            filas.Count,
            filas.Count(f => f.Estado == EstadoTrayectoTemporal.Cargado),
            filas.Count(f => f.Estado == EstadoTrayectoTemporal.Validado),
            filas.Count(f => f.Estado == EstadoTrayectoTemporal.Procesado),
            filas.Count(f => f.Estado == EstadoTrayectoTemporal.Error),
            visibles
                .OrderBy(f => f.CodigoRuta, StringComparer.OrdinalIgnoreCase)
                .ThenBy(f => f.OrdenParada, StringComparer.OrdinalIgnoreCase)
                .Select(ToOutput)
                .ToList());
    }

    private static TrayectoTemporalOutputDto ToOutput(TrayectoTemporal fila) => new(
        fila.Id,
        fila.IdCargue,
        fila.CodigoRuta,
        fila.IdentificacionConductor,
        fila.PlacaVehiculo,
        fila.Ubicacion,
        fila.OrdenParada,
        fila.Latitud,
        fila.Longitud,
        fila.LoginUsuario,
        fila.PersonaId,
        fila.VehiculoId,
        fila.Estado,
        fila.Observacion,
        fila.CreatedAtUtc,
        fila.UpdatedAtUtc);
}
