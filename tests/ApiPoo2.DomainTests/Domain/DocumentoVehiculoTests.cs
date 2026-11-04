using ApiPoo2.Domain.BlacklistedTokens;
using ApiPoo2.Domain.Common;
using ApiPoo2.Domain.TiposDocumento;
using FluentAssertions;

namespace ApiPoo2.DomainTests.Domain;

public sealed class DocumentoVehiculoTests
{
    private static readonly DateTime Ahora = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static readonly byte[] PdfValido = "%PDF-1.7 contenido"u8.ToArray();

    private static DocumentoVehiculo Crear(
        Guid? vehiculoId = null,
        Guid? tipoDocumentoId = null,
        DateTime? expedicion = null,
        DateTime? vencimiento = null)
        => DocumentoVehiculo.Crear(
            vehiculoId ?? Guid.NewGuid(),
            tipoDocumentoId ?? Guid.NewGuid(),
            PdfValido,
            "SOAT.pdf",
            expedicion ?? Ahora.AddDays(-10),
            vencimiento ?? Ahora.AddDays(30),
            Ahora);

    [Fact]
    public void Crear_AsignaTodosLosCamposEnVerificacion()
    {
        var vehiculoId = Guid.NewGuid();
        var tipoDocumentoId = Guid.NewGuid();

        var relacion = Crear(vehiculoId, tipoDocumentoId, Ahora.AddDays(-10), Ahora.AddDays(30));

        relacion.VehiculoId.Should().Be(vehiculoId);
        relacion.TipoDocumentoId.Should().Be(tipoDocumentoId);
        relacion.NombreArchivo.Value.Should().Be("SOAT.pdf");
        relacion.FechaExpedicion.Should().Be(Ahora.AddDays(-10));
        relacion.FechaVencimiento.Should().Be(Ahora.AddDays(30));
        relacion.Estado.Should().Be(EstadoDocumento.EnVerificacion);
        relacion.CreatedAtUtc.Should().Be(Ahora);
        relacion.UpdatedAtUtc.Should().BeNull();
    }

    [Fact]
    public void Crear_RechazaVehiculoVacio()
        => FluentActions.Invoking(() => Crear(vehiculoId: Guid.Empty))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("vehiculo_documento.vehiculo");

    [Fact]
    public void Crear_RechazaTipoDocumentoVacio()
        => FluentActions.Invoking(() => Crear(tipoDocumentoId: Guid.Empty))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("vehiculo_documento.documento");

    [Fact]
    public void Crear_RechazaVencimientoNoPosteriorAExpedicion()
    {
        FluentActions.Invoking(() => Crear(expedicion: Ahora, vencimiento: Ahora))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("vehiculo_documento.vencimiento");

        FluentActions.Invoking(() => Crear(expedicion: Ahora, vencimiento: Ahora.AddDays(-1)))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("vehiculo_documento.vencimiento");
    }

    [Fact]
    public void Crear_RechazaExpedicionFutura()
        => FluentActions.Invoking(() => Crear(expedicion: Ahora.AddDays(1), vencimiento: Ahora.AddDays(30)))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("vehiculo_documento.expedicion");

    [Fact]
    public void ReemplazarContenido_ActualizaArchivoYVuelveAEnVerificacion()
    {
        var relacion = Crear();
        relacion.CambiarEstado(EstadoDocumento.Habilitado, Ahora);

        relacion.ReemplazarContenido(
            "%PDF-2.0 nuevo contenido"u8.ToArray(),
            "SOAT-v2.pdf",
            Ahora.AddDays(-5),
            Ahora.AddDays(60),
            Ahora);

        relacion.NombreArchivo.Value.Should().Be("SOAT-v2.pdf");
        relacion.Contenido.ToArray().Should().Equal("%PDF-2.0 nuevo contenido"u8.ToArray());
        relacion.FechaExpedicion.Should().Be(Ahora.AddDays(-5));
        relacion.FechaVencimiento.Should().Be(Ahora.AddDays(60));
        relacion.Estado.Should().Be(EstadoDocumento.EnVerificacion);
        relacion.UpdatedAtUtc.Should().Be(Ahora);
    }

    [Fact]
    public void ReemplazarContenido_ConservaElContentTypeOriginal()
    {
        var relacion = Crear();
        var contentTypeOriginal = relacion.Contenido.ContentType;

        relacion.ReemplazarContenido(
            "%PDF-2.0"u8.ToArray(), "SOAT-v2.pdf", Ahora.AddDays(-5), Ahora.AddDays(60), Ahora);

        relacion.Contenido.ContentType.Should().Be(contentTypeOriginal);
    }

    [Fact]
    public void ReemplazarContenido_RechazaVencimientoNoPosteriorAExpedicion()
    {
        var relacion = Crear();

        FluentActions.Invoking(() => relacion.ReemplazarContenido(
                PdfValido, "SOAT-v2.pdf", Ahora, Ahora.AddDays(-1), Ahora))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("vehiculo_documento.vencimiento");
    }

    [Fact]
    public void ReemplazarContenido_RechazaExpedicionFutura()
    {
        var relacion = Crear();

        FluentActions.Invoking(() => relacion.ReemplazarContenido(
                PdfValido, "SOAT-v2.pdf", Ahora.AddDays(1), Ahora.AddDays(30), Ahora))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("vehiculo_documento.expedicion");
    }

    [Fact]
    public void EstadoActual_DerivaVencidoSoloPorFecha()
    {
        var relacion = Crear();

        relacion.EstadoActual(Ahora.AddDays(29)).Should().Be(EstadoDocumento.EnVerificacion);
        relacion.EstadoActual(Ahora.AddDays(30)).Should().Be(EstadoDocumento.Vencido);
        relacion.Estado.Should().Be(EstadoDocumento.EnVerificacion);
    }

    [Fact]
    public void CambiarEstado_CambiaYMarcaActualizado()
    {
        var relacion = Crear();

        relacion.CambiarEstado(EstadoDocumento.Habilitado, Ahora.AddDays(1));

        relacion.Estado.Should().Be(EstadoDocumento.Habilitado);
        relacion.UpdatedAtUtc.Should().Be(Ahora.AddDays(1));
    }

    [Fact]
    public void CambiarEstado_NoMarcaActualizadoSiNoCambia()
    {
        var relacion = Crear();

        relacion.CambiarEstado(EstadoDocumento.EnVerificacion, Ahora.AddDays(1));

        relacion.UpdatedAtUtc.Should().BeNull();
    }

    [Fact]
    public void CambiarEstado_RechazaAsignarVencido()
    {
        var relacion = Crear();

        FluentActions.Invoking(() => relacion.CambiarEstado(EstadoDocumento.Vencido, Ahora))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("vehiculo_documento.estado");
    }
}

public sealed class BlacklistedTokenTests
{
    private static readonly DateTime Ahora = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_AsignaTodosLosCamposEInicializaIdentidad()
    {
        var jti = Guid.NewGuid();
        var personaId = Guid.NewGuid();
        var expiracion = Ahora.AddHours(1);

        var token = BlacklistedToken.Create(jti, personaId, expiracion, Ahora);

        token.Jti.Should().Be(jti);
        token.PersonaId.Should().Be(personaId);
        token.ExpiresAtUtc.Should().Be(expiracion);
        token.RevokedAtUtc.Should().Be(Ahora);
        token.Id.Should().NotBeEmpty();
        token.CreatedAtUtc.Should().Be(Ahora);
    }

    [Fact]
    public void Create_RechazaJtiVacio()
        => FluentActions.Invoking(() => BlacklistedToken.Create(
                Guid.Empty, Guid.NewGuid(), Ahora.AddHours(1), Ahora))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("access.blacklist_invalid");

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_RechazaTokenYaExpirado(int horasRestantes)
        => FluentActions.Invoking(() => BlacklistedToken.Create(
                Guid.NewGuid(), Guid.NewGuid(), Ahora.AddHours(horasRestantes), Ahora))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("access.blacklist_invalid");

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    public void IsExpired_ComparaContraLaExpiracion(int horasRestantes, bool esperado)
        => BlacklistedToken.Create(
                Guid.NewGuid(), Guid.NewGuid(), Ahora.AddHours(1), Ahora)
            .IsExpired(Ahora.AddHours(horasRestantes))
            .Should().Be(esperado);
}
