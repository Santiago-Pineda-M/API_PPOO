using ApiPoo2.Domain.Rutas;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Trayectos.Jobs;

/// <summary>
///     Tarea programada de geocodificación: toma las paradas sin coordenadas, consulta el puerto
///     <see cref="IGeocodingService" /> y persiste las coordenadas en la ruta. Los errores de una
///     parada no cortan el ciclo: queda pendiente para la próxima ejecución.
/// </summary>
public sealed class GeocodificarParadasUseCase : BaseUseCase<SinParametros, int>
{
    private readonly IRutaRepository _rutaRepository;
    private readonly IGeocodingService _geocodingService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly GeocodingOptions _options;
    private readonly ILogger<GeocodificarParadasUseCase> _logger;

    public GeocodificarParadasUseCase(
        IEnumerable<FluentValidation.IValidator<SinParametros>> validators,
        ILoggerFactory loggerFactory,
        IRutaRepository rutaRepository,
        IGeocodingService geocodingService,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork,
        GeocodingOptions options)
        : base(validators, loggerFactory)
    {
        _rutaRepository = rutaRepository;
        _geocodingService = geocodingService;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
        _options = options;
        _logger = loggerFactory.CreateLogger<GeocodificarParadasUseCase>();
    }

    protected override async Task<int> ExecuteCoreAsync(
        SinParametros request,
        CancellationToken cancellationToken)
    {
        var limite = Math.Max(1, _options.MaxPorCiclo);
        var pendientes = await _rutaRepository.GetPendientesCoordenadasAsync(
            limite,
            cancellationToken);
        var rutas = new Dictionary<Guid, Ruta>();
        var asignadas = 0;

        foreach (var pendiente in pendientes)
        {
            Coordenadas? coordenadas;

            try
            {
                coordenadas = await _geocodingService.GeocodeAsync(
                    pendiente.Ubicacion,
                    cancellationToken);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(
                    ex,
                    "No se pudo geocodificar la parada {ParadaId} ({Ubicacion})",
                    pendiente.ParadaId,
                    pendiente.Ubicacion);
                coordenadas = null;
            }

            if (coordenadas is null)
            {
                continue;
            }

            if (!rutas.TryGetValue(pendiente.RutaId, out var ruta))
            {
                ruta = await _rutaRepository.GetByIdAsync(pendiente.RutaId, cancellationToken);

                if (ruta is null)
                {
                    continue;
                }

                rutas[pendiente.RutaId] = ruta;
            }

            ruta.AsignarCoordenadas(
                pendiente.ParadaId,
                coordenadas.Latitud,
                coordenadas.Longitud,
                _dateTimeProvider.UtcNow);
            asignadas++;

            if (_options.PausaEntreLlamadasMs > 0)
            {
                await Task.Delay(_options.PausaEntreLlamadasMs, cancellationToken);
            }
        }

        if (asignadas > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return asignadas;
    }
}
