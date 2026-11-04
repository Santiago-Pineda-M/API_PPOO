using ApiPoo2.Domain.Common;
using ApiPoo2.Domain.TiposDocumento;
using ApiPoo2.Domain.Personas;
using ApiPoo2.Domain.Vehiculos;
using FluentAssertions;

namespace ApiPoo2.UnitTests.Domain;

public sealed class VehiculoTests
{
    private static readonly DateTime Ahora = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static readonly byte[] PdfValido = "%PDF-1.7 contenido"u8.ToArray();

    private static Vehiculo CrearVehiculo(
        string placa = "ABC123",
        TipoVehiculo tipo = TipoVehiculo.Automovil)
        => Vehiculo.Register(
            placa,
            tipo,
            TipoServicio.Privado,
            TipoCombustible.Gasolina,
            5,
            "#FF5733",
            2020,
            "Toyota",
            "Fortuner SW",
            Ahora);

    private static TipoDocumento CrearTipoDocumento(string tipos = "AM", string obligatoriedad = "RR")
        => TipoDocumento.Register("SOAT", "SOAT", tipos, obligatoriedad, "Seguro Obligatorio", Ahora);

    [Fact]
    public void Register_NormalizaLaPlaca()
        => CrearVehiculo("abc123").Placa.Value.Should().Be("ABC123");

    [Fact]
    public void Register_AsignaTodosLosCampos()
    {
        var vehiculo = CrearVehiculo();

        vehiculo.TipoVehiculo.Should().Be(TipoVehiculo.Automovil);
        vehiculo.TipoServicio.Should().Be(TipoServicio.Privado);
        vehiculo.Combustible.Should().Be(TipoCombustible.Gasolina);
        vehiculo.CapacidadPasajeros.Should().Be(5);
        vehiculo.Color.Value.Should().Be("#FF5733");
        vehiculo.Modelo.Should().Be(2020);
        vehiculo.Marca.Value.Should().Be("Toyota");
        vehiculo.Linea.Value.Should().Be("Fortuner SW");
    }

    // Placa de automóvil: tres letras + tres números.
    [Theory]
    [InlineData("ABC123")]
    [InlineData("XYZ789")]
    public void PlacaAutomovil_AceptaTresLetrasYTresNumeros(string placa)
        => Placa.From(placa, TipoVehiculo.Automovil).Value.Should().Be(placa);

    // Placa de motocicleta: tres letras + dos números + una letra.
    [Theory]
    [InlineData("ABC12D")]
    [InlineData("XYZ98K")]
    public void PlacaMotocicleta_AceptaTresLetrasDosNumerosYUnaLetra(string placa)
        => Placa.From(placa, TipoVehiculo.Motocicleta).Value.Should().Be(placa);

    [Theory]
    [InlineData("AB1234")]      // 6 caracteres pero formato de automóvil
    [InlineData("123ABC")]
    [InlineData("ABC-12")]
    [InlineData("ABC12")]
    public void Placa_RechazaFormatosInvalidos(string placa)
        => FluentActions.Invoking(() => Placa.From(placa, TipoVehiculo.Automovil))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("vehiculo.placa");

    [Fact]
    public void Placa_RechazaLargoDistintoDeSeis()
        => FluentActions.Invoking(() => Placa.From("ABC1234", TipoVehiculo.Automovil))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("vehiculo.placa");

    [Fact]
    public void Placa_RechazaFormatoDeMotocicletaParaAutomovil()
        => FluentActions.Invoking(() => Placa.From("ABC12D", TipoVehiculo.Automovil))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("vehiculo.placa");

    [Fact]
    public void Placa_RechazaFormatoDeAutomovilParaMotocicleta()
        => FluentActions.Invoking(() => Placa.From("ABC123", TipoVehiculo.Motocicleta))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("vehiculo.placa");

    [Theory]
    [InlineData("ABC123")]
    [InlineData("ABC12D")]
    public void PlacaFromCualquiera_AceptaAmbosFormatosParaBusqueda(string placa)
        => Placa.FromCualquiera(placa).Value.Should().Be(placa);

    [Fact]
    public void PlacaFromCualquiera_RechazaFormatoInvalido()
        => FluentActions.Invoking(() => Placa.FromCualquiera("AB1234"))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("vehiculo.placa");

    [Fact]
    public void Register_RechazaCapacidadNegativa()
        => FluentActions.Invoking(() => Vehiculo.Register(
                "ABC123", TipoVehiculo.Automovil, TipoServicio.Privado, TipoCombustible.Gas,
                -1, "#FFFFFF", 2020, "Toyota", "X", Ahora))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("vehiculo.capacidad");

    [Fact]
    public void Register_RechazaModeloNoPositivo()
        => FluentActions.Invoking(() => Vehiculo.Register(
                "ABC123", TipoVehiculo.Automovil, TipoServicio.Privado, TipoCombustible.Gas,
                5, "#FFFFFF", 0, "Toyota", "X", Ahora))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("vehiculo.modelo");

    [Theory]
    [InlineData("FF5733")]     // sin #
    [InlineData("#FF573")]      // 5 dígitos
    [InlineData("#GG5733")]     // no hexadecimal
    [InlineData("#FF5733FF")]   // 8 dígitos
    public void Color_RechazaFormatosInvalidos(string color)
        => FluentActions.Invoking(() => Color.From(color))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("vehiculo.color");

    [Fact]
    public void Actualizar_CambiaCamposSinCambiarTipo()
    {
        var vehiculo = CrearVehiculo();

        vehiculo.Actualizar(
            "XYZ789",
            TipoVehiculo.Automovil,
            TipoServicio.Publico,
            TipoCombustible.Disel,
            7,
            "#112233",
            2021,
            "Nissan",
            "Versa",
            Ahora);

        vehiculo.Placa.Value.Should().Be("XYZ789");
        vehiculo.TipoServicio.Should().Be(TipoServicio.Publico);
        vehiculo.Combustible.Should().Be(TipoCombustible.Disel);
        vehiculo.CapacidadPasajeros.Should().Be(7);
        vehiculo.UpdatedAtUtc.Should().Be(Ahora);
    }

    [Fact]
    public void Actualizar_RechazaCambioDeTipoConDocumentosAsociados()
    {
        var vehiculo = CrearVehiculo();
        vehiculo.AdjuntarDocumento(
            CrearTipoDocumento(), PdfValido, "SOAT.pdf", Ahora.AddDays(-10), Ahora.AddDays(30), Ahora);

        FluentActions.Invoking(() => vehiculo.Actualizar(
                "ABC12D",
                TipoVehiculo.Motocicleta,
                TipoServicio.Privado,
                TipoCombustible.Gasolina,
                2,
                "#112233",
                2021,
                "Yamaha",
                "FZ",
                Ahora))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("vehiculo.tipo.cambio_con_documentos");
    }

    [Fact]
    public void GuardarDocumento_ActualizaArchivoExistente()
    {
        var vehiculo = CrearVehiculo();
        var documento = CrearTipoDocumento();
        vehiculo.AdjuntarDocumento(documento, PdfValido, "SOAT.pdf", Ahora.AddDays(-10), Ahora.AddDays(30), Ahora);

        var actualizado = vehiculo.GuardarDocumento(
            documento,
            "%PDF-2.0 nuevo"u8.ToArray(),
            "SOAT-v2.pdf",
            Ahora.AddDays(-9),
            Ahora.AddDays(60),
            Ahora);

        actualizado.Estado.Should().Be(EstadoDocumento.EnVerificacion);
        actualizado.NombreArchivo.Value.Should().Be("SOAT-v2.pdf");
        actualizado.FechaExpedicion.Should().Be(Ahora.AddDays(-9));
        actualizado.FechaVencimiento.Should().Be(Ahora.AddDays(60));
    }

    [Fact]
    public void NuevoVehiculo_NoTieneDocumentos()
        => CrearVehiculo().TieneDocumentos().Should().BeFalse();

    [Fact]
    public void AdjuntarDocumento_NaceEnVerificacion()
    {
        var vehiculo = CrearVehiculo();

        var relacion = vehiculo.AdjuntarDocumento(
            CrearTipoDocumento(), PdfValido, "SOAT.pdf", Ahora.AddDays(-10), Ahora.AddDays(30), Ahora);

        relacion.Estado.Should().Be(EstadoDocumento.EnVerificacion);
        relacion.FechaExpedicion.Should().Be(Ahora.AddDays(-10));
        vehiculo.TieneDocumentos().Should().BeTrue();
    }

    [Fact]
    public void AdjuntarDocumento_RechazaDocumentoDeOtroTipoDeVehiculo()
    {
        var moto = CrearVehiculo("ABC12D", TipoVehiculo.Motocicleta);
        var soloAutomovil = CrearTipoDocumento(tipos: "A");

        FluentActions.Invoking(() => moto.AdjuntarDocumento(
                soloAutomovil, PdfValido, "SOAT.pdf", Ahora.AddDays(-1), Ahora.AddDays(30), Ahora))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("vehiculo.documento.incompatible");
    }

    [Fact]
    public void AdjuntarDocumento_RechazaDuplicado()
    {
        var vehiculo = CrearVehiculo();
        var documento = CrearTipoDocumento();
        vehiculo.AdjuntarDocumento(documento, PdfValido, "SOAT.pdf", Ahora.AddDays(-1), Ahora.AddDays(30), Ahora);

        FluentActions.Invoking(() => vehiculo.AdjuntarDocumento(
                documento, PdfValido, "SOAT.pdf", Ahora.AddDays(-1), Ahora.AddDays(30), Ahora))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("vehiculo.documento.duplicado");
    }

    [Fact]
    public void AdjuntarDocumento_RechazaVencimientoAnteriorALaExpedicion()
    {
        var vehiculo = CrearVehiculo();

        FluentActions.Invoking(() => vehiculo.AdjuntarDocumento(
                CrearTipoDocumento(), PdfValido, "SOAT.pdf", Ahora, Ahora.AddDays(-5), Ahora))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("vehiculo_documento.vencimiento");
    }

    [Fact]
    public void AdjuntarDocumento_RechazaExpedicionFutura()
    {
        var vehiculo = CrearVehiculo();

        FluentActions.Invoking(() => vehiculo.AdjuntarDocumento(
                CrearTipoDocumento(), PdfValido, "SOAT.pdf", Ahora.AddDays(5), Ahora.AddDays(30), Ahora))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("vehiculo_documento.expedicion");
    }

    [Fact]
    public void EstadoActual_DerivaVencidoPorFecha()
    {
        var relacion = CrearVehiculo().AdjuntarDocumento(
            CrearTipoDocumento(), PdfValido, "SOAT.pdf", Ahora.AddDays(-10), Ahora.AddDays(30), Ahora);

        relacion.EstadoActual(Ahora).Should().Be(EstadoDocumento.EnVerificacion);
        relacion.EstadoActual(Ahora.AddDays(31)).Should().Be(EstadoDocumento.Vencido);
    }

    [Fact]
    public void AsociarConductor_NaceEnEsperaDeAprobacion()
        => CrearVehiculo().AsociarConductor(Guid.NewGuid(), Ahora)
            .Estado.Should().Be(EstadoConductor.Ea);

    [Fact]
    public void AsociarConductor_RechazaDuplicado()
    {
        var vehiculo = CrearVehiculo();
        var personaId = Guid.NewGuid();
        vehiculo.AsociarConductor(personaId, Ahora);

        FluentActions.Invoking(() => vehiculo.AsociarConductor(personaId, Ahora))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("vehiculo.conductor.duplicado");
    }
}

public sealed class TipoDocumentoParametricoTests
{
    private static readonly DateTime Ahora = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData("a", "A")]
    [InlineData("m", "M")]
    [InlineData("am", "AM")]
    public void TiposAplicables_AceptaSoloValoresDelEnunciado(string entrada, string esperado)
        => TiposVehiculoAplicables.From(entrada).Value.Should().Be(esperado);

    [Theory]
    [InlineData("X")]
    [InlineData("MA")]
    [InlineData("AMA")]
    public void TiposAplicables_RechazaValoresNoValidos(string entrada)
        => FluentActions.Invoking(() => TiposVehiculoAplicables.From(entrada))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("documento.tipos_aplicables");

    [Theory]
    [InlineData("ra", "RA")]
    [InlineData("rm", "RM")]
    [InlineData("rr", "RR")]
    public void Obligatoriedad_AceptaSoloValoresDelEnunciado(string entrada, string esperado)
        => CodigoObligatoriedad.From(entrada).Value.Should().Be(esperado);

    [Theory]
    [InlineData("RAX")]
    [InlineData("AA")]
    public void Obligatoriedad_RechazaValoresNoValidos(string entrada)
        => FluentActions.Invoking(() => CodigoObligatoriedad.From(entrada))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("documento.obligatoriedad");

    [Fact]
    public void AplicaA_SoloAlTipoIndicado()
    {
        var soloAuto = TipoDocumento.Register("SOAT", "SOAT", "A", "RA", "desc", Ahora);

        soloAuto.AplicaA(TipoVehiculo.Automovil).Should().BeTrue();
        soloAuto.AplicaA(TipoVehiculo.Motocicleta).Should().BeFalse();
    }

    [Fact]
    public void AplicaA_AMBetaParaAmbos()
    {
        var ambos = TipoDocumento.Register("RT", "Registro", "AM", "RR", "desc", Ahora);

        ambos.AplicaA(TipoVehiculo.Automovil).Should().BeTrue();
        ambos.AplicaA(TipoVehiculo.Motocicleta).Should().BeTrue();
    }

    [Fact]
    public void EsObligatorioPara_RaSoloAutomovil()
    {
        var doc = TipoDocumento.Register("SOAT", "SOAT", "A", "RA", "desc", Ahora);

        doc.EsObligatorioPara(TipoVehiculo.Automovil).Should().BeTrue();
        doc.EsObligatorioPara(TipoVehiculo.Motocicleta).Should().BeFalse();
    }

    [Fact]
    public void EsObligatorioPara_RmSoloMotocicleta()
    {
        var doc = TipoDocumento.Register("TM", "Tecnomecanica", "M", "RM", "desc", Ahora);

        doc.EsObligatorioPara(TipoVehiculo.Motocicleta).Should().BeTrue();
        doc.EsObligatorioPara(TipoVehiculo.Automovil).Should().BeFalse();
    }

    [Fact]
    public void ActualizarDocumentoParametrico_CambiaDatosSinCambiarCodigo()
    {
        var documento = TipoDocumento.Register("SOAT", "SOAT", "A", "RA", "desc", Ahora);

        documento.Actualizar("Seguro obligatorio", "AM", "RR", "Cobertura amplia", Ahora);

        documento.Codigo.Value.Should().Be("SOAT");
        documento.Nombre.Value.Should().Be("Seguro obligatorio");
        documento.TiposVehiculoAplicables.Value.Should().Be("AM");
        documento.CodigoObligatoriedad.Value.Should().Be("RR");
        documento.Descripcion.Value.Should().Be("Cobertura amplia");
    }

    [Fact]
    public void Descripcion_EsObligatoria()
        => FluentActions.Invoking(() => TipoDocumento.Register("SOAT", "SOAT", "A", "RA", "  ", Ahora))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("documento.descripcion");
}

public sealed class DocumentoContenidoTests
{
    [Fact]
    public void Contenido_RechazaVacio()
        => FluentActions.Invoking(() => ContenidoDocumento.From([], "doc.pdf"))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("documento.contenido");

    [Fact]
    public void Contenido_RechazaNoPdf()
        => FluentActions.Invoking(() => ContenidoDocumento.From("texto plano"u8.ToArray(), "doc.txt"))
            .Should().Throw<DomainValidationException>();

    [Fact]
    public void Contenido_AceptaPdfPorFirma()
        => ContenidoDocumento.From("%PDF-1.7"u8.ToArray(), "cualquier.bin").Length.Should().Be(8);

    [Fact]
    public void Contenido_FromBase64_HaceRoundTrip()
    {
        var bytes = "%PDF-1.7 datos"u8.ToArray();
        var base64 = Convert.ToBase64String(bytes);

        var contenido = ContenidoDocumento.FromBase64(base64, "doc.pdf");

        contenido.ToArray().Should().Equal(bytes);
        contenido.ToBase64().Should().Be(base64);
    }

    [Fact]
    public void Contenido_RechazaBase64Invalido()
        => FluentActions.Invoking(() => ContenidoDocumento.FromBase64("no-es-base64!!", "doc.pdf"))
            .Should().Throw<DomainValidationException>();

    [Fact]
    public void Contenido_RechazaBase64Vacio()
        => FluentActions.Invoking(() => ContenidoDocumento.FromBase64("  ", "doc.pdf"))
            .Should().Throw<DomainValidationException>();
}