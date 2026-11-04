using ApiPoo2.Application.Exceptions;
using ApiPoo2.Application.IServices;
using ApiPoo2.Application.UseCases.Cargues;
using ApiPoo2.ApplicationTests.Common.TestDoubles;
using ApiPoo2.Domain.Trayectos;
using FluentAssertions;

namespace ApiPoo2.ApplicationTests.Cargues;

public sealed class CarguesUseCaseTests
{
    private const long IdCargueEsperado = 202601011200;

    private static readonly DateTime Ahora = TestData.Ahora;

    private static FilaTrayectoExcel Fila(
        string codigo = "R-001",
        string orden = "0",
        string? latitud = null,
        string? longitud = null,
        string identificacion = "1234567890",
        string placa = "ABC123",
        string login = "admin",
        string ubicacion = "Terminal de Ibagué")
        => new(codigo, identificacion, placa, ubicacion, orden, latitud, longitud, login);

    private static CargarTrayectosExcelUseCase CrearCargar(
        FakeExcelTrayectoParser parser,
        FakeCargueTrayectosProcedures procedures,
        FakeTrayectoTemporalRepository? trayectos = null,
        CargueOptions? options = null)
        => new(
            [],
            TestData.LoggerFactory,
            parser,
            procedures,
            trayectos ?? new FakeTrayectoTemporalRepository(),
            new FakeDateTimeProvider { UtcNow = Ahora },
            options ?? new CargueOptions());

    private static CargarTrayectosExcelInputDto Archivo(long tamanio = 1024, string nombre = "trayectos.xlsx")
        => new(new MemoryStream(), nombre, tamanio);

    [Fact]
    public async Task CargarTrayectos_ArchivoValido_EjecutaProcedimientosYResume()
    {
        var parser = new FakeExcelTrayectoParser();
        parser.Filas.Add(Fila(orden: "0"));
        parser.Filas.Add(Fila(orden: "1", ubicacion: "Plaza de Bolívar"));
        var procedures = new FakeCargueTrayectosProcedures
        {
            Resumen = new ResumenCargue(2, 2, 2, 0),
        };

        var resultado = await CrearCargar(parser, procedures).ExecuteAsync(
            Archivo(),
            CancellationToken.None);

        resultado.IdCargue.Should().Be(IdCargueEsperado);
        resultado.Total.Should().Be(2);
        resultado.Cargados.Should().Be(0);
        resultado.Validados.Should().Be(0);
        resultado.Procesados.Should().Be(2);
        resultado.Errores.Should().Be(0);
        procedures.UltimoIdCargue.Should().Be(IdCargueEsperado);
        procedures.UltimasFilas.Should().HaveCount(2);
        parser.UltimoNombreArchivo.Should().Be("trayectos.xlsx");
    }

    [Fact]
    public async Task CargarTrayectos_ExtensionInvalida_RechazaConBadRequest()
    {
        var act = () => CrearCargar(
                new FakeExcelTrayectoParser(),
                new FakeCargueTrayectosProcedures())
            .ExecuteAsync(Archivo(nombre: "trayectos.csv"), CancellationToken.None);

        (await act.Should().ThrowAsync<BadRequestException>())
            .Which.Code.Should().Be("cargue.archivo.formato");
    }

    [Fact]
    public async Task CargarTrayectos_ArchivoVacio_RechazaConBadRequest()
    {
        var act = () => CrearCargar(
                new FakeExcelTrayectoParser(),
                new FakeCargueTrayectosProcedures())
            .ExecuteAsync(Archivo(tamanio: 0), CancellationToken.None);

        (await act.Should().ThrowAsync<BadRequestException>())
            .Which.Code.Should().Be("cargue.archivo.vacio");
    }

    [Fact]
    public async Task CargarTrayectos_ArchivoDemasiadoGrande_RechazaConBadRequest()
    {
        var act = () => CrearCargar(
                new FakeExcelTrayectoParser(),
                new FakeCargueTrayectosProcedures(),
                options: new CargueOptions { TamanioMaximoMb = 1 })
            .ExecuteAsync(Archivo(tamanio: 2 * 1024 * 1024), CancellationToken.None);

        (await act.Should().ThrowAsync<BadRequestException>())
            .Which.Code.Should().Be("cargue.archivo.tamanio");
    }

    [Fact]
    public async Task CargarTrayectos_ParserFalla_RechazaConBadRequest()
    {
        var parser = new FakeExcelTrayectoParser
        {
            ThrowOnParse = new InvalidDataException("Hoja inválida."),
        };

        var act = () => CrearCargar(parser, new FakeCargueTrayectosProcedures())
            .ExecuteAsync(Archivo(), CancellationToken.None);

        (await act.Should().ThrowAsync<BadRequestException>())
            .Which.Code.Should().Be("cargue.archivo.formato");
    }

    [Fact]
    public async Task CargarTrayectos_SinFilas_RechazaConBadRequest()
    {
        var act = () => CrearCargar(
                new FakeExcelTrayectoParser(),
                new FakeCargueTrayectosProcedures())
            .ExecuteAsync(Archivo(), CancellationToken.None);

        (await act.Should().ThrowAsync<BadRequestException>())
            .Which.Code.Should().Be("cargue.archivo.vacio");
    }

    [Fact]
    public async Task CargarTrayectos_ExcedeElMaximoDeFilas_RechazaConBadRequest()
    {
        var parser = new FakeExcelTrayectoParser();
        parser.Filas.Add(Fila());
        parser.Filas.Add(Fila(orden: "1"));

        var act = () => CrearCargar(
                parser,
                new FakeCargueTrayectosProcedures(),
                options: new CargueOptions { MaxFilas = 1 })
            .ExecuteAsync(Archivo(), CancellationToken.None);

        (await act.Should().ThrowAsync<BadRequestException>())
            .Which.Code.Should().Be("cargue.archivo.filas");
    }

    [Fact]
    public async Task CargarTrayectos_FilasInvalidas_AcumulaLosErrores()
    {
        var parser = new FakeExcelTrayectoParser();
        parser.Filas.Add(new FilaTrayectoExcel(null, null, null, null, "9", "x", null, null));

        var act = () => CrearCargar(parser, new FakeCargueTrayectosProcedures())
            .ExecuteAsync(Archivo(), CancellationToken.None);

        var excepcion = (await act.Should().ThrowAsync<BadRequestException>()).Which;

        excepcion.Code.Should().Be("cargue.columnas.invalidas");
        excepcion.Errors.Should().NotBeEmpty();
        excepcion.Errors.Should().Contain(e => e.Contains("código de ruta"));
        excepcion.Errors.Should().Contain(e => e.Contains("orden de parada"));
        excepcion.Errors.Should().Contain(e => e.Contains("latitud no es un número válido"));
    }

    [Fact]
    public async Task CargarTrayectos_RutaSinParadaInicial_RechazaConBadRequest()
    {
        var parser = new FakeExcelTrayectoParser();
        parser.Filas.Add(Fila(orden: "1"));

        var act = () => CrearCargar(parser, new FakeCargueTrayectosProcedures())
            .ExecuteAsync(Archivo(), CancellationToken.None);

        var excepcion = (await act.Should().ThrowAsync<BadRequestException>()).Which;

        excepcion.Code.Should().Be("cargue.columnas.invalidas");
        excepcion.Errors.Should().Contain(e => e.Contains("parada inicial"));
    }

    [Fact]
    public async Task CargarTrayectos_RutaConDatosIncoherentes_RechazaConBadRequest()
    {
        var parser = new FakeExcelTrayectoParser();
        parser.Filas.Add(Fila(orden: "0", identificacion: "1234567890"));
        parser.Filas.Add(Fila(orden: "1", identificacion: "9876543210", placa: "XYZ987", login: "otro"));

        var act = () => CrearCargar(parser, new FakeCargueTrayectosProcedures())
            .ExecuteAsync(Archivo(), CancellationToken.None);

        var excepcion = (await act.Should().ThrowAsync<BadRequestException>()).Which;

        excepcion.Code.Should().Be("cargue.columnas.invalidas");
        excepcion.Errors.Should().Contain(e => e.Contains("conductores distintos"));
        excepcion.Errors.Should().Contain(e => e.Contains("vehículos distintos"));
        excepcion.Errors.Should().Contain(e => e.Contains("logins distintos"));
    }

    [Fact]
    public async Task CargarTrayectos_CargueDuplicado_RechazaConConflict()
    {
        var parser = new FakeExcelTrayectoParser();
        parser.Filas.Add(Fila());
        var trayectos = new FakeTrayectoTemporalRepository();
        trayectos.Items.Add(TrayectoTemporal.Crear(
            IdCargueEsperado, "R-001", "1", "ABC123", "Terminal", "0", null, null, "admin", Ahora));

        var act = () => CrearCargar(parser, new FakeCargueTrayectosProcedures(), trayectos)
            .ExecuteAsync(Archivo(), CancellationToken.None);

        (await act.Should().ThrowAsync<ConflictException>()).Which.Code.Should().Be("cargue.duplicado");
    }

    private static TrayectoTemporal FilaCargada(
        long idCargue,
        string codigo,
        EstadoTrayectoTemporal estado,
        string? observacion = null,
        string orden = "0")
    {
        var fila = TrayectoTemporal.Crear(
            idCargue, codigo, "1234567890", "ABC123", "Terminal", orden, null, null, "admin", Ahora);

        switch (estado)
        {
            case EstadoTrayectoTemporal.Validado:
                fila.MarcarValidado(Guid.NewGuid(), Guid.NewGuid(), Ahora);
                break;
            case EstadoTrayectoTemporal.Procesado:
                fila.MarcarValidado(Guid.NewGuid(), Guid.NewGuid(), Ahora);
                fila.MarcarProcesado(Ahora);
                break;
            case EstadoTrayectoTemporal.Error:
                fila.MarcarError(observacion ?? "Error de validación.", Ahora);
                break;
        }

        return fila;
    }

    [Fact]
    public async Task ListarCargues_DevuelveElResumenMasRecientePrimero()
    {
        var trayectos = new FakeTrayectoTemporalRepository();
        trayectos.Items.Add(FilaCargada(202601011200, "R-001", EstadoTrayectoTemporal.Procesado));
        trayectos.Items.Add(FilaCargada(202601021200, "R-002", EstadoTrayectoTemporal.Error, "Falla"));

        var useCase = new ListarCarguesUseCase([], TestData.LoggerFactory, trayectos);

        var resultado = await useCase.ExecuteAsync(new ListarCarguesInputDto(), CancellationToken.None);

        resultado.Should().HaveCount(2);
        resultado[0].IdCargue.Should().Be(202601021200);
        resultado[0].Errores.Should().Be(1);
        resultado[1].Procesados.Should().Be(1);
    }

    [Fact]
    public async Task ListarCargues_ConEstado_FiltraLosResumenesSinEseEstado()
    {
        var trayectos = new FakeTrayectoTemporalRepository();
        trayectos.Items.Add(FilaCargada(202601011200, "R-001", EstadoTrayectoTemporal.Procesado));
        trayectos.Items.Add(FilaCargada(202601021200, "R-002", EstadoTrayectoTemporal.Error, "Falla"));

        var useCase = new ListarCarguesUseCase([], TestData.LoggerFactory, trayectos);

        var resultado = await useCase.ExecuteAsync(
            new ListarCarguesInputDto(EstadoTrayectoTemporal.Error),
            CancellationToken.None);

        resultado.Should().ContainSingle().Which.IdCargue.Should().Be(202601021200);
    }

    [Fact]
    public async Task GetCargue_Inexistente_RechazaConNotFound()
    {
        var useCase = new GetCargueUseCase(
            [],
            TestData.LoggerFactory,
            new FakeTrayectoTemporalRepository());

        var act = () => useCase.ExecuteAsync(new GetCargueInputDto(1), CancellationToken.None);

        (await act.Should().ThrowAsync<NotFoundException>()).Which.Code.Should().Be("cargue.not_found");
    }

    [Fact]
    public async Task GetCargue_Existente_ResumeLosEstadosYOrdenaLasFilas()
    {
        var trayectos = new FakeTrayectoTemporalRepository();
        trayectos.Items.Add(FilaCargada(IdCargueEsperado, "R-002", EstadoTrayectoTemporal.Procesado));
        trayectos.Items.Add(FilaCargada(IdCargueEsperado, "R-001", EstadoTrayectoTemporal.Error, "Falla"));
        trayectos.Items.Add(FilaCargada(IdCargueEsperado, "R-001", EstadoTrayectoTemporal.Validado, orden: "1"));

        var useCase = new GetCargueUseCase([], TestData.LoggerFactory, trayectos);

        var resultado = await useCase.ExecuteAsync(
            new GetCargueInputDto(IdCargueEsperado),
            CancellationToken.None);

        resultado.Total.Should().Be(3);
        resultado.Procesados.Should().Be(1);
        resultado.Errores.Should().Be(1);
        resultado.Validados.Should().Be(1);
        resultado.Filas.Should().HaveCount(3);
        resultado.Filas[0].CodigoRuta.Should().Be("R-001");
        resultado.Filas[0].Estado.Should().Be(EstadoTrayectoTemporal.Error);
        resultado.Filas[0].Observacion.Should().Be("Falla");
    }

    [Fact]
    public async Task GetCargue_ConEstado_DevuelveSoloEseEstadoPeroMantieneElResumen()
    {
        var trayectos = new FakeTrayectoTemporalRepository();
        trayectos.Items.Add(FilaCargada(IdCargueEsperado, "R-001", EstadoTrayectoTemporal.Procesado));
        trayectos.Items.Add(FilaCargada(IdCargueEsperado, "R-002", EstadoTrayectoTemporal.Error, "Falla"));

        var useCase = new GetCargueUseCase([], TestData.LoggerFactory, trayectos);

        var resultado = await useCase.ExecuteAsync(
            new GetCargueInputDto(IdCargueEsperado, EstadoTrayectoTemporal.Error),
            CancellationToken.None);

        resultado.Total.Should().Be(2);
        resultado.Filas.Should().ContainSingle().Which.CodigoRuta.Should().Be("R-002");
    }

    [Fact]
    public async Task EliminarCargue_Existente_EliminaLasFilas()
    {
        var trayectos = new FakeTrayectoTemporalRepository();
        trayectos.Items.Add(FilaCargada(IdCargueEsperado, "R-001", EstadoTrayectoTemporal.Procesado));

        var useCase = new EliminarCargueUseCase([], TestData.LoggerFactory, trayectos);

        await useCase.ExecuteAsync(new EliminarCargueInputDto(IdCargueEsperado), CancellationToken.None);

        trayectos.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task EliminarCargue_Inexistente_RechazaConNotFound()
    {
        var useCase = new EliminarCargueUseCase(
            [],
            TestData.LoggerFactory,
            new FakeTrayectoTemporalRepository());

        var act = () => useCase.ExecuteAsync(new EliminarCargueInputDto(1), CancellationToken.None);

        (await act.Should().ThrowAsync<NotFoundException>()).Which.Code.Should().Be("cargue.not_found");
    }
}
