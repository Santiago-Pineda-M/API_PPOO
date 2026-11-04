using System.Globalization;
using ApiPoo2.Domain.Trayectos;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.Application.UseCases.Cargues;

/// <summary>
///     Carga masiva del Excel de trayectos. El parser solo extrae celdas; acá se transforma y valida
///     la estructura (columnas obligatorias, orden, coordenadas y coherencia por ruta) y después se
///     delegan el cargue, la validación y el procesado masivo a los tres procedimientos almacenados.
/// </summary>
public sealed class CargarTrayectosExcelUseCase
    : BaseUseCase<CargarTrayectosExcelInputDto, CargarTrayectosExcelOutputDto>
{
    private const int MaximoParadasPorRuta = 7;
    private const int OrdenInicial = 0;
    private const int OrdenMaximo = 6;

    private readonly IExcelTrayectoParser _parser;
    private readonly ICargueTrayectosProcedures _procedures;
    private readonly ITrayectoTemporalRepository _trayectoRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly CargueOptions _options;

    public CargarTrayectosExcelUseCase(
        IEnumerable<FluentValidation.IValidator<CargarTrayectosExcelInputDto>> validators,
        ILoggerFactory loggerFactory,
        IExcelTrayectoParser parser,
        ICargueTrayectosProcedures procedures,
        ITrayectoTemporalRepository trayectoRepository,
        IDateTimeProvider dateTimeProvider,
        CargueOptions options)
        : base(validators, loggerFactory)
    {
        _parser = parser;
        _procedures = procedures;
        _trayectoRepository = trayectoRepository;
        _dateTimeProvider = dateTimeProvider;
        _options = options;
    }

    protected override async Task<CargarTrayectosExcelOutputDto> ExecuteCoreAsync(
        CargarTrayectosExcelInputDto request,
        CancellationToken cancellationToken)
    {
        ValidarArchivo(request);

        IReadOnlyList<FilaTrayectoExcel> crudas;
        try
        {
            crudas = _parser.Parse(request.Contenido, request.NombreArchivo);
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException)
        {
            throw new BadRequestException(
                "cargue.archivo.formato",
                "El archivo no se pudo leer como un Excel .xlsx válido.",
                [ex.Message]);
        }

        if (crudas.Count == 0)
        {
            throw new BadRequestException(
                "cargue.archivo.vacio",
                "El archivo no contiene filas para cargar.");
        }

        if (crudas.Count > _options.MaxFilas)
        {
            throw new BadRequestException(
                "cargue.archivo.filas",
                $"El archivo no puede superar las {_options.MaxFilas} filas.");
        }

        var filas = Transformar(crudas);
        var ahora = _dateTimeProvider.UtcNow;
        var idCargue = long.Parse(
            ahora.ToString("yyyyMMddHHmm", CultureInfo.InvariantCulture),
            CultureInfo.InvariantCulture);

        if (await _trayectoRepository.ExisteCargueAsync(idCargue, cancellationToken))
        {
            throw new ConflictException(
                "cargue.duplicado",
                "Ya existe un cargue registrado en este minuto. Esperá un minuto e intentá de nuevo.");
        }

        var resumen = await _procedures.EjecutarAsync(idCargue, filas, cancellationToken);

        // El procesado consume los registros que la validación dejó en VALIDADO: para que el
        // resumen no cuente dos veces la misma fila, se descuentan los ya procesados.
        var validadosPendientes = Math.Max(0, resumen.Validados - resumen.Procesados);

        return new CargarTrayectosExcelOutputDto(
            idCargue,
            ahora,
            resumen.Total,
            Math.Max(0, resumen.Total - resumen.Validados - resumen.Errores),
            validadosPendientes,
            resumen.Procesados,
            resumen.Errores);
    }

    private void ValidarArchivo(CargarTrayectosExcelInputDto request)
    {
        if (request.Contenido is null)
        {
            throw new BadRequestException("cargue.archivo.vacio", "El archivo es obligatorio.");
        }

        var extension = Path.GetExtension(request.NombreArchivo);

        if (!string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            throw new BadRequestException(
                "cargue.archivo.formato",
                "El archivo debe tener extensión .xlsx.");
        }

        if (request.TamanioBytes <= 0)
        {
            throw new BadRequestException("cargue.archivo.vacio", "El archivo está vacío.");
        }

        var maxBytes = _options.TamanioMaximoMb * 1024L * 1024L;

        if (request.TamanioBytes > maxBytes)
        {
            throw new BadRequestException(
                "cargue.archivo.tamanio",
                $"El archivo no puede superar los {_options.TamanioMaximoMb} MB.");
        }
    }

    /// <summary>
    ///     Transformación de la fila cruda a una fila tipada y coherente. Todos los errores se
    ///     acumulan para devolverlos juntos en una sola respuesta 400.
    /// </summary>
    private static IReadOnlyList<FilaTrayectoExcel> Transformar(
        IReadOnlyList<FilaTrayectoExcel> crudas)
    {
        var errores = new List<string>();
        var filas = new List<FilaTrayectoExcel>(crudas.Count);

        for (var i = 0; i < crudas.Count; i++)
        {
            var cruda = crudas[i];
            var numeroFila = i + 2;
            var codigo = Texto(cruda.CodigoRuta);
            var identificacion = Texto(cruda.IdentificacionConductor);
            var placa = Texto(cruda.PlacaVehiculo)?.ToUpperInvariant();
            var ubicacion = Texto(cruda.Ubicacion);
            var ordenTexto = Texto(cruda.OrdenParada);
            var latitudTexto = Texto(cruda.Latitud);
            var longitudTexto = Texto(cruda.Longitud);
            var login = Texto(cruda.LoginUsuario);

            if (codigo is null)
            {
                errores.Add($"Fila {numeroFila}: el código de ruta es obligatorio.");
            }

            if (identificacion is null)
            {
                errores.Add($"Fila {numeroFila}: la identificación del conductor es obligatoria.");
            }

            if (placa is null)
            {
                errores.Add($"Fila {numeroFila}: la placa del vehículo es obligatoria.");
            }

            if (ubicacion is null)
            {
                errores.Add($"Fila {numeroFila}: la ubicación es obligatoria.");
            }

            if (login is null)
            {
                errores.Add($"Fila {numeroFila}: el login del usuario es obligatorio.");
            }

            if (ordenTexto is null
                || !int.TryParse(
                    ordenTexto,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var orden)
                || orden is < OrdenInicial or > OrdenMaximo)
            {
                errores.Add($"Fila {numeroFila}: el orden de parada debe ser un entero entre 0 y 6.");
            }

            var latitud = LeerCoordenada(
                latitudTexto,
                -90,
                90,
                "latitud",
                numeroFila,
                errores);
            var longitud = LeerCoordenada(
                longitudTexto,
                -180,
                180,
                "longitud",
                numeroFila,
                errores);

            if (latitud.HasValue != longitud.HasValue)
            {
                errores.Add($"Fila {numeroFila}: la latitud y la longitud deben venir juntas o ambas vacías.");
            }

            filas.Add(new FilaTrayectoExcel(
                codigo?.ToUpperInvariant(),
                identificacion,
                placa,
                ubicacion,
                ordenTexto,
                latitudTexto,
                longitudTexto,
                login));
        }

        ValidarCoherenciaPorRuta(filas, errores);

        if (errores.Count > 0)
        {
            throw new BadRequestException(
                "cargue.columnas.invalidas",
                "El archivo contiene filas inválidas.",
                errores);
        }

        return filas;
    }

    private static void ValidarCoherenciaPorRuta(
        IReadOnlyList<FilaTrayectoExcel> filas,
        List<string> errores)
    {
        foreach (var grupo in filas.GroupBy(f => f.CodigoRuta!, StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(grupo.Key))
            {
                continue;
            }

            var lista = grupo.ToList();

            if (lista.Count > MaximoParadasPorRuta)
            {
                errores.Add(
                    $"La ruta '{grupo.Key}' tiene {lista.Count} paradas; el máximo es {MaximoParadasPorRuta}.");
            }

            if (lista.Select(f => f.IdentificacionConductor)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count() > 1)
            {
                errores.Add($"La ruta '{grupo.Key}' tiene conductores distintos en sus filas.");
            }

            if (lista.Select(f => f.PlacaVehiculo)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count() > 1)
            {
                errores.Add($"La ruta '{grupo.Key}' tiene vehículos distintos en sus filas.");
            }

            if (lista.Select(f => f.LoginUsuario)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count() > 1)
            {
                errores.Add($"La ruta '{grupo.Key}' tiene logins distintos en sus filas.");
            }

            var ordenes = lista
                .Select(f => int.TryParse(f.OrdenParada, NumberStyles.Integer, CultureInfo.InvariantCulture, out var o)
                    ? o
                    : (int?)null)
                .Where(o => o.HasValue)
                .Select(o => o!.Value)
                .ToList();

            if (ordenes.Count != ordenes.Distinct().Count())
            {
                errores.Add($"La ruta '{grupo.Key}' repite el orden de alguna parada.");
            }

            if (!ordenes.Contains(OrdenInicial))
            {
                errores.Add($"La ruta '{grupo.Key}' no incluye la parada inicial (orden 0).");
            }
        }
    }

    private static double? LeerCoordenada(
        string? texto,
        double minimo,
        double maximo,
        string nombre,
        int numeroFila,
        List<string> errores)
    {
        if (texto is null)
        {
            return null;
        }

        if (!double.TryParse(
                texto,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var valor))
        {
            errores.Add($"Fila {numeroFila}: la {nombre} no es un número válido.");
            return null;
        }

        if (valor < minimo || valor > maximo)
        {
            errores.Add($"Fila {numeroFila}: la {nombre} debe estar entre {minimo} y {maximo}.");
            return null;
        }

        return valor;
    }

    private static string? Texto(string? valor)
        => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
