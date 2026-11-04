using ApiPoo2.Infrastructure.Excel;
using ClosedXML.Excel;
using FluentAssertions;

namespace ApiPoo2.InfrastructureTests.Excel;

public sealed class ExcelTrayectoParserTests
{
    private static readonly string[] Encabezados =
    [
        "codigo_ruta",
        "identificacion_conductor",
        "placa_vehiculo",
        "ubicacion",
        "orden_parada",
        "latitud",
        "longitud",
        "login_usuario",
    ];

    [Fact]
    public void Parse_ExtraeLasCeldasRecortadasSinTransformarTipos()
    {
        var contenido = CrearExcel(
            [" Codigo Ruta ", "identificacion_conductor", "placa_vehiculo", "ubicacion",
                "orden_parada", "latitud", "longitud", "login_usuario"],
            ["  RUTA-01  ", " 123456 ", " ABC123 ", " Terminal Norte ", " 0 ", " 6.24 ", " -75.58 ", " rm1 "],
            ["RUTA-01", "123456", "ABC123", "Parque Principal", "1", "", "", "rm1"]);

        var filas = new ExcelTrayectoParser().Parse(new MemoryStream(contenido), "trayectos.xlsx");

        filas.Should().HaveCount(2);

        var primera = filas[0];
        primera.CodigoRuta.Should().Be("RUTA-01");
        primera.IdentificacionConductor.Should().Be("123456");
        primera.PlacaVehiculo.Should().Be("ABC123");
        primera.Ubicacion.Should().Be("Terminal Norte");
        primera.OrdenParada.Should().Be("0");
        primera.Latitud.Should().Be("6.24");
        primera.Longitud.Should().Be("-75.58");
        primera.LoginUsuario.Should().Be("rm1");

        filas[1].Latitud.Should().BeNull();
        filas[1].Longitud.Should().BeNull();
    }

    [Fact]
    public void Parse_IgnoraFilasCompletamenteVacias()
    {
        var contenido = CrearExcel(
            Encabezados,
            ["RUTA-01", "123456", "ABC123", "Terminal Norte", "0", "", "", "rm1"],
            ["", "", "", "", "", "", "", ""],
            ["RUTA-01", "123456", "ABC123", "Parque", "1", "", "", "rm1"]);

        var filas = new ExcelTrayectoParser().Parse(new MemoryStream(contenido), "trayectos.xlsx");

        filas.Should().HaveCount(2);
    }

    [Fact]
    public void Parse_FallaSiFaltanColumnasObligatorias()
    {
        var contenido = CrearExcel(["codigo_ruta", "placa_vehiculo"], ["RUTA-01", "ABC123"]);

        var accion = () => new ExcelTrayectoParser().Parse(new MemoryStream(contenido), "incompleto.xlsx");

        accion.Should().Throw<InvalidDataException>()
            .WithMessage("*identificacion_conductor*");
    }

    [Fact]
    public void Parse_FallaSiLaHojaNoTieneFilasDeDatos()
    {
        var contenido = CrearExcel(Encabezados);

        var accion = () => new ExcelTrayectoParser().Parse(new MemoryStream(contenido), "vacio.xlsx");

        accion.Should().Throw<InvalidDataException>()
            .WithMessage("*no contiene filas*");
    }

    [Fact]
    public void Parse_FallaSiElContenidoNoEsUnExcel()
    {
        var contenido = "%PDF-1.4 no es excel"u8.ToArray();

        var accion = () => new ExcelTrayectoParser().Parse(new MemoryStream(contenido), "archivo.xlsx");

        accion.Should().Throw<InvalidDataException>()
            .WithMessage("*no es un Excel*");
    }

    [Fact]
    public void Parse_LaPlantillaPublicada_SeLeeConElParserReal()
    {
        var ruta = Path.Combine(AppContext.BaseDirectory, "Excel", "plantilla-trayectos.xlsx");

        using var stream = File.OpenRead(ruta);
        var filas = new ExcelTrayectoParser().Parse(stream, "plantilla-trayectos.xlsx");

        filas.Should().HaveCount(2);
        filas.Select(f => f.CodigoRuta).Should().OnlyContain(codigo => codigo == "RUTA-EJEMPLO");
        filas.Select(f => f.OrdenParada).Should().Equal("0", "1");
    }

    private static byte[] CrearExcel(string[] encabezados, params string[][] filas)
    {
        using var libro = new XLWorkbook();
        var hoja = libro.Worksheets.Add("Trayectos");

        for (var columna = 0; columna < encabezados.Length; columna++)
        {
            hoja.Cell(1, columna + 1).Value = encabezados[columna];
        }

        for (var fila = 0; fila < filas.Length; fila++)
        {
            for (var columna = 0; columna < filas[fila].Length; columna++)
            {
                hoja.Cell(fila + 2, columna + 1).Value = filas[fila][columna];
            }
        }

        using var stream = new MemoryStream();
        libro.SaveAs(stream);

        return stream.ToArray();
    }
}
