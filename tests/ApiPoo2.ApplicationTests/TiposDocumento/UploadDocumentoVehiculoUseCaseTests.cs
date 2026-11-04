using ApiPoo2.Application.Exceptions;
using ApiPoo2.ApplicationTests.Common.TestDoubles;
using ApiPoo2.Application.UseCases.TiposDocumento;
using ApiPoo2.Domain.TiposDocumento;
using ApiPoo2.Domain.Vehiculos;
using FluentAssertions;

namespace ApiPoo2.ApplicationTests.TiposDocumento;

public sealed class UploadDocumentoVehiculoUseCaseTests
{
    private static readonly DateTime Ahora = TestData.Ahora;

    private static Vehiculo CrearVehiculo()
        => Vehiculo.Register(
            "ABC123",
            TipoVehiculo.Automovil,
            TipoServicio.Privado,
            TipoCombustible.Gasolina,
            5,
            "#FF5733",
            2020,
            "Toyota",
            "Fortuner SW",
            Ahora);

    private static TipoDocumento CrearTipoDocumento(string codigo = "SOAT")
        => TipoDocumento.Register(codigo, codigo, "AM", "RR", "Seguro Obligatorio", Ahora);

    private static DocumentoVehiculoUploadItem CrearItem(TipoDocumento documento, string nombreArchivo = "SOAT.pdf")
        => new(
            documento.Id,
            Convert.ToBase64String(TestData.PdfValido),
            nombreArchivo,
            Ahora.AddDays(-10),
            Ahora.AddDays(30));

    private static UploadDocumentoVehiculoUseCase CrearUseCase(
        FakeVehiculoRepository vehiculos,
        FakeTipoDocumentoRepository documentos,
        FakeUnitOfWork? unitOfWork = null)
        => new(
            [],
            TestData.LoggerFactory,
            vehiculos,
            documentos,
            new FakeDateTimeProvider { UtcNow = Ahora },
            unitOfWork ?? new FakeUnitOfWork());

    [Fact]
    public async Task SinDocumentos_RechazaConRequestValidation()
    {
        var vehiculo = CrearVehiculo();
        var useCase = CrearUseCase(new FakeVehiculoRepository { }, new FakeTipoDocumentoRepository());

        var act = () => useCase.ExecuteAsync(
            new UploadDocumentoVehiculoInputDto(vehiculo.Id, []),
            CancellationToken.None);

        await act.Should().ThrowAsync<RequestValidationException>();
    }

    [Fact]
    public async Task VehiculoInexistente_RechazaConVehicleNotFound()
    {
        var useCase = CrearUseCase(new FakeVehiculoRepository(), new FakeTipoDocumentoRepository());

        var act = () => useCase.ExecuteAsync(
            new UploadDocumentoVehiculoInputDto(
                Guid.NewGuid(),
                [new DocumentoVehiculoUploadItem(
                    Guid.NewGuid(), Convert.ToBase64String(TestData.PdfValido), "SOAT.pdf", Ahora, Ahora.AddDays(30))]),
            CancellationToken.None);

        (await act.Should().ThrowAsync<NotFoundException>()).Which.Code.Should().Be("vehicle.not_found");
    }

    [Fact]
    public async Task TipoDocumentoInexistente_RechazaConDocumentNotFound()
    {
        var vehiculo = CrearVehiculo();
        var useCase = CrearUseCase(
            new FakeVehiculoRepository { }.WithVehiculo(vehiculo),
            new FakeTipoDocumentoRepository());

        var act = () => useCase.ExecuteAsync(
            new UploadDocumentoVehiculoInputDto(
                vehiculo.Id,
                [new DocumentoVehiculoUploadItem(Guid.NewGuid(), Convert.ToBase64String(TestData.PdfValido), "SOAT.pdf", Ahora, Ahora.AddDays(30))]),
            CancellationToken.None);

        (await act.Should().ThrowAsync<NotFoundException>()).Which.Code.Should().Be("document.not_found");
    }

    [Fact]
    public async Task DocumentoValido_AsociaEnEnVerificacionYPersiste()
    {
        var vehiculo = CrearVehiculo();
        var tipoDocumento = CrearTipoDocumento();
        var documentos = new FakeTipoDocumentoRepository();
        documentos.Add(tipoDocumento);
        var unitOfWork = new FakeUnitOfWork();

        var useCase = CrearUseCase(
            new FakeVehiculoRepository { }.WithVehiculo(vehiculo),
            documentos,
            unitOfWork);

        var resultado = await useCase.ExecuteAsync(
            new UploadDocumentoVehiculoInputDto(vehiculo.Id, [CrearItem(tipoDocumento)]),
            CancellationToken.None);

        resultado.VehiculoId.Should().Be(vehiculo.Id);
        resultado.Cargados.Should().ContainSingle();
        resultado.Cargados[0].TipoDocumentoId.Should().Be(tipoDocumento.Id);
        resultado.Cargados[0].NombreArchivo.Should().Be("SOAT.pdf");
        resultado.Cargados[0].Estado.Should().Be(EstadoDocumento.EnVerificacion);
        vehiculo.TieneDocumentos().Should().BeTrue();
        unitOfWork.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task VariosDocumentos_LosCargaEnUnaSolaLlamada()
    {
        var vehiculo = CrearVehiculo();
        var soat = CrearTipoDocumento("SOAT");
        var revision = CrearTipoDocumento("RT");
        var documentos = new FakeTipoDocumentoRepository();
        documentos.Add(soat);
        documentos.Add(revision);

        var useCase = CrearUseCase(
            new FakeVehiculoRepository { }.WithVehiculo(vehiculo),
            documentos);

        var resultado = await useCase.ExecuteAsync(
            new UploadDocumentoVehiculoInputDto(
                vehiculo.Id,
                [CrearItem(soat), CrearItem(revision, "RT.pdf")]),
            CancellationToken.None);

        resultado.Cargados.Should().HaveCount(2);
        resultado.Cargados.Select(c => c.TipoDocumentoId).Should().BeEquivalentTo([soat.Id, revision.Id]);
    }

    [Fact]
    public async Task DocumentoYaAsociado_ActualizaElArchivoYVuelveAEnVerificacion()
    {
        var vehiculo = CrearVehiculo();
        var tipoDocumento = CrearTipoDocumento();
        vehiculo.AdjuntarDocumento(
            tipoDocumento,
            TestData.PdfValido,
            "SOAT.pdf",
            Ahora.AddDays(-10),
            Ahora.AddDays(30),
            Ahora);

        var documentos = new FakeTipoDocumentoRepository();
        documentos.Add(tipoDocumento);

        var useCase = CrearUseCase(
            new FakeVehiculoRepository { }.WithVehiculo(vehiculo),
            documentos);

        var nuevoPdf = "%PDF-2.0 contenido nuevo"u8.ToArray();
        var resultado = await useCase.ExecuteAsync(
            new UploadDocumentoVehiculoInputDto(
                vehiculo.Id,
                [new DocumentoVehiculoUploadItem(
                    tipoDocumento.Id, Convert.ToBase64String(nuevoPdf), "SOAT-v2.pdf", Ahora.AddDays(-5), Ahora.AddDays(60))]),
            CancellationToken.None);

        resultado.Cargados.Should().ContainSingle();
        resultado.Cargados[0].Estado.Should().Be(EstadoDocumento.EnVerificacion);
        resultado.Cargados[0].NombreArchivo.Should().Be("SOAT-v2.pdf");
        documentos.DocumentosVehiculo.Should().BeEmpty();
    }

    [Fact]
    public async Task DocumentoDeOtroTipoDeVehiculo_RechazaPorInvarianteDeDominio()
    {
        var moto = Vehiculo.Register(
            "ABC12D", TipoVehiculo.Motocicleta, TipoServicio.Privado, TipoCombustible.Gasolina,
            2, "#FF5733", 2020, "Yamaha", "FZ", Ahora);
        var soloAutomovil = TipoDocumento.Register("SOAT", "SOAT", "A", "RA", "desc", Ahora);
        var documentos = new FakeTipoDocumentoRepository();
        documentos.Add(soloAutomovil);

        var useCase = CrearUseCase(
            new FakeVehiculoRepository { }.WithVehiculo(moto),
            documentos);

        var act = () => useCase.ExecuteAsync(
            new UploadDocumentoVehiculoInputDto(moto.Id, [CrearItem(soloAutomovil)]),
            CancellationToken.None);

        await act.Should().ThrowAsync<ApiPoo2.Domain.Common.DomainValidationException>();
    }

    [Fact]
    public async Task Base64Invalido_RechazaPorInvarianteDeDominio()
    {
        var vehiculo = CrearVehiculo();
        var tipoDocumento = CrearTipoDocumento();
        var documentos = new FakeTipoDocumentoRepository();
        documentos.Add(tipoDocumento);

        var useCase = CrearUseCase(
            new FakeVehiculoRepository { }.WithVehiculo(vehiculo),
            documentos);

        var act = () => useCase.ExecuteAsync(
            new UploadDocumentoVehiculoInputDto(
                vehiculo.Id,
                [new DocumentoVehiculoUploadItem(tipoDocumento.Id, "no-es-base64!!", "SOAT.pdf", Ahora, Ahora.AddDays(30))]),
            CancellationToken.None);

        await act.Should().ThrowAsync<ApiPoo2.Domain.Common.DomainValidationException>();
    }

    [Fact]
    public async Task VencimientoAnteriorALaExpedicion_RechazaPorInvarianteDeDominio()
    {
        var vehiculo = CrearVehiculo();
        var tipoDocumento = CrearTipoDocumento();
        var documentos = new FakeTipoDocumentoRepository();
        documentos.Add(tipoDocumento);

        var useCase = CrearUseCase(
            new FakeVehiculoRepository { }.WithVehiculo(vehiculo),
            documentos);

        var act = () => useCase.ExecuteAsync(
            new UploadDocumentoVehiculoInputDto(
                vehiculo.Id,
                [new DocumentoVehiculoUploadItem(
                    tipoDocumento.Id, Convert.ToBase64String(TestData.PdfValido), "SOAT.pdf", Ahora, Ahora.AddDays(-5))]),
            CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ApiPoo2.Domain.Common.DomainValidationException>();
        exception.Which.Code.Should().Be("vehiculo_documento.vencimiento");
    }
}

internal static class FakeVehiculoRepositoryExtensions
{
    public static FakeVehiculoRepository WithVehiculo(this FakeVehiculoRepository repository, Vehiculo vehiculo)
    {
        repository.Add(vehiculo);
        return repository;
    }
}
