namespace ApiPoo2.Application.IServices;

/// <summary>Fila cruda extraída del Excel de cargue, todavía sin validar por el caso de uso.</summary>
public sealed record FilaTrayectoExcel(
    string? CodigoRuta,
    string? IdentificacionConductor,
    string? PlacaVehiculo,
    string? Ubicacion,
    string? OrdenParada,
    string? Latitud,
    string? Longitud,
    string? LoginUsuario);

/// <summary>
///     Extracción de celdas del archivo Excel. No conoce reglas de negocio ni entidades: eso es
///     responsabilidad del caso de uso, según la separación de capas del proyecto.
/// </summary>
public interface IExcelTrayectoParser
{
    IReadOnlyList<FilaTrayectoExcel> Parse(Stream contenido, string nombreArchivo);
}
