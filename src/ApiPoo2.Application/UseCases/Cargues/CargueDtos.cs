using ApiPoo2.Domain.Trayectos;

namespace ApiPoo2.Application.UseCases.Cargues;

public sealed record CargarTrayectosExcelInputDto(
    Stream Contenido,
    string NombreArchivo,
    long TamanioBytes);

public sealed record CargarTrayectosExcelOutputDto(
    long IdCargue,
    DateTime FechaCargue,
    int Total,
    int Cargados,
    int Validados,
    int Procesados,
    int Errores);

public sealed record ListarCarguesInputDto(EstadoTrayectoTemporal? Estado = null);

public sealed record CargueResumenOutputDto(
    long IdCargue,
    DateTime FechaCargue,
    int Total,
    int Cargados,
    int Validados,
    int Procesados,
    int Errores);

public sealed record GetCargueInputDto(
    long IdCargue,
    EstadoTrayectoTemporal? Estado = null);

public sealed record TrayectoTemporalOutputDto(
    Guid Id,
    long IdCargue,
    string? CodigoRuta,
    string? IdentificacionConductor,
    string? PlacaVehiculo,
    string? Ubicacion,
    string? OrdenParada,
    string? Latitud,
    string? Longitud,
    string? LoginUsuario,
    Guid? PersonaId,
    Guid? VehiculoId,
    EstadoTrayectoTemporal Estado,
    string? Observacion,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record GetCargueOutputDto(
    long IdCargue,
    DateTime FechaCargue,
    int Total,
    int Cargados,
    int Validados,
    int Procesados,
    int Errores,
    IReadOnlyList<TrayectoTemporalOutputDto> Filas);

public sealed record EliminarCargueInputDto(long IdCargue);
