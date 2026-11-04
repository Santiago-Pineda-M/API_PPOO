using System.Globalization;
using ApiPoo2.Application.IServices;
using ClosedXML.Excel;

namespace ApiPoo2.Infrastructure.Excel;

/// <summary>
///     Extracción de celdas del Excel de cargue con ClosedXML. No aplica reglas de negocio: solo lee
///     los encabezados, valida que estén las columnas obligatorias y devuelve los valores como texto.
/// </summary>
public sealed class ExcelTrayectoParser : IExcelTrayectoParser
{
    private static readonly string[] ColumnasObligatorias =
    [
        "codigo_ruta",
        "identificacion_conductor",
        "placa_vehiculo",
        "ubicacion",
        "orden_parada",
        "login_usuario",
    ];

    public IReadOnlyList<FilaTrayectoExcel> Parse(Stream contenido, string nombreArchivo)
    {
        if (contenido is null)
        {
            throw new InvalidDataException("El contenido del archivo es obligatorio.");
        }

        XLWorkbook workbook;

        try
        {
            workbook = new XLWorkbook(contenido);
        }
        catch (Exception ex)
        {
            throw new InvalidDataException(
                $"El archivo '{nombreArchivo}' no es un Excel .xlsx válido.",
                ex);
        }

        using (workbook)
        {
            var hoja = workbook.Worksheets.FirstOrDefault()
                ?? throw new InvalidDataException("El archivo no contiene ninguna hoja.");

            var filaEncabezado = hoja.FirstRowUsed()
                ?? throw new InvalidDataException("La hoja está vacía.");

            var columnas = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var celda in filaEncabezado.CellsUsed())
            {
                var nombre = NormalizarEncabezado(celda.GetString());

                if (!string.IsNullOrWhiteSpace(nombre) && !columnas.ContainsKey(nombre))
                {
                    columnas[nombre] = celda.Address.ColumnNumber;
                }
            }

            var faltantes = ColumnasObligatorias.Where(c => !columnas.ContainsKey(c)).ToList();

            if (faltantes.Count > 0)
            {
                throw new InvalidDataException(
                    $"Faltan columnas obligatorias: {string.Join(", ", faltantes)}.");
            }

            var ultimaFila = hoja.LastRowUsed()?.RowNumber() ?? filaEncabezado.RowNumber();
            var filas = new List<FilaTrayectoExcel>();

            for (var numeroFila = filaEncabezado.RowNumber() + 1; numeroFila <= ultimaFila; numeroFila++)
            {
                var valores = ColumnasObligatorias
                    .Concat(["latitud", "longitud"])
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        columna => columna,
                        columna => columnas.TryGetValue(columna, out var indice)
                            ? Texto(hoja.Cell(numeroFila, indice))
                            : null,
                        StringComparer.OrdinalIgnoreCase);

                if (valores.Values.All(string.IsNullOrWhiteSpace))
                {
                    continue;
                }

                filas.Add(new FilaTrayectoExcel(
                    valores["codigo_ruta"],
                    valores["identificacion_conductor"],
                    valores["placa_vehiculo"],
                    valores["ubicacion"],
                    valores["orden_parada"],
                    valores.GetValueOrDefault("latitud"),
                    valores.GetValueOrDefault("longitud"),
                    valores["login_usuario"]));
            }

            if (filas.Count == 0)
            {
                throw new InvalidDataException("La hoja no contiene filas de datos.");
            }

            return filas;
        }
    }

    private static string NormalizarEncabezado(string? encabezado)
        => string.IsNullOrWhiteSpace(encabezado)
            ? string.Empty
            : encabezado.Trim().ToLowerInvariant().Replace(' ', '_');

    private static string? Texto(IXLCell celda)
    {
        if (celda.IsEmpty())
        {
            return null;
        }

        var texto = celda.Value.ToString(CultureInfo.InvariantCulture);

        return string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
    }
}
