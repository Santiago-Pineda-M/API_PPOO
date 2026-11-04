using ApiPoo2.Domain.Common;
using ApiPoo2.Domain.Personas;
using FluentAssertions;

namespace ApiPoo2.DomainTests.Domain;

public sealed class LicenciaConductorTests
{
    private static readonly DateTime Ahora = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static readonly byte[] PdfValido = "%PDF-1.7 contenido"u8.ToArray();

    private static Persona CrearConductor()
        => Persona.Register(
            TipoIdentificacion.CedulaCiudadania,
            "1234567890",
            "Carlos",
            "Ríos",
            "carlos@example.com",
            TipoPersona.Conductor,
            Ahora);

    [Fact]
    public void LicenciaConduccion_ClonaElContenidoYValidaFirmaPdf()
    {
        var original = "%PDF-1.7 contenido"u8.ToArray();

        var licencia = LicenciaConduccion.From(original);
        original[0] = (byte)'X';

        licencia.Length.Should().Be("%PDF-1.7 contenido"u8.Length);
        licencia.ToArray()[0].Should().Be((byte)'%');
    }

    [Fact]
    public void LicenciaConduccion_RechazaContenidoVacio()
        => FluentActions.Invoking(() => LicenciaConduccion.From([]))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("persona.licencia.formato");

    [Fact]
    public void LicenciaConduccion_RechazaContenidoQueNoEsPdf()
        => FluentActions.Invoking(() => LicenciaConduccion.From("PNG"u8.ToArray()))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("persona.licencia.formato");

    [Fact]
    public void LicenciaConduccion_RechazaArchivoDemasiadoGrande()
    {
        var grande = new byte[LicenciaConduccion.MaxBytes + 1];
        "%PDF"u8.CopyTo(grande);

        var act = () => LicenciaConduccion.From(grande);

        act.Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("persona.licencia.formato");
    }

    [Fact]
    public void LicenciaConduccion_FromBase64_DecodificaUnPdf()
    {
        var base64 = Convert.ToBase64String(PdfValido);

        var licencia = LicenciaConduccion.FromBase64(base64);

        licencia.Length.Should().Be(PdfValido.Length);
    }

    [Fact]
    public void LicenciaConduccion_FromBase64_RechazaContenidoVacio()
        => FluentActions.Invoking(() => LicenciaConduccion.FromBase64("  "))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("persona.licencia.formato");

    [Fact]
    public void LicenciaConduccion_FromBase64_RechazaBase64Invalido()
        => FluentActions.Invoking(() => LicenciaConduccion.FromBase64("no-es-base64"))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("persona.licencia.formato");

    [Fact]
    public void RegistrarLicencia_AsignaContenidoYVigencia()
    {
        var persona = CrearConductor();
        var vigencia = Ahora.AddMonths(6);

        persona.RegistrarLicencia(PdfValido, vigencia, Ahora);

        persona.TieneLicencia().Should().BeTrue();
        persona.Licencia!.Length.Should().Be(PdfValido.Length);
        persona.FechaVigenciaLicencia.Should().Be(vigencia);
        persona.LicenciaVigente(Ahora).Should().BeTrue();
        persona.LicenciaVigente(vigencia).Should().BeFalse();
        persona.UpdatedAtUtc.Should().Be(Ahora);
    }

    [Fact]
    public void RegistrarLicencia_RechazaPersonaQueNoEsConductor()
    {
        var administrativo = Persona.Register(
            TipoIdentificacion.CedulaCiudadania,
            "9999999999",
            "Ana",
            "Gómez",
            "ana@example.com",
            TipoPersona.Administrativo,
            Ahora);

        var act = () => administrativo.RegistrarLicencia(PdfValido, Ahora.AddMonths(1), Ahora);

        act.Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("persona.licencia.tipo_persona");
    }

    [Fact]
    public void RegistrarLicencia_RechazaVigenciaPorDefecto()
    {
        var persona = CrearConductor();

        var act = () => persona.RegistrarLicencia(PdfValido, default, Ahora);

        act.Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("persona.licencia.vigencia");
    }

    [Fact]
    public void RegistrarLicencia_RechazaVigenciaPasada()
    {
        var persona = CrearConductor();

        var act = () => persona.RegistrarLicencia(PdfValido, Ahora.AddDays(-1), Ahora);

        act.Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("persona.licencia.vigencia");
    }

    [Fact]
    public void EliminarLicencia_QuitaElContenidoYLaVigencia()
    {
        var persona = CrearConductor();
        persona.RegistrarLicencia(PdfValido, Ahora.AddMonths(1), Ahora);

        persona.EliminarLicencia(Ahora.AddDays(1));

        persona.TieneLicencia().Should().BeFalse();
        persona.FechaVigenciaLicencia.Should().BeNull();
        persona.UpdatedAtUtc.Should().Be(Ahora.AddDays(1));
    }

    [Fact]
    public void EliminarLicencia_SinLicencia_NoCambiaLaFecha()
    {
        var persona = CrearConductor();

        persona.EliminarLicencia(Ahora.AddDays(1));

        persona.TieneLicencia().Should().BeFalse();
        persona.UpdatedAtUtc.Should().BeNull();
    }

    [Fact]
    public void CambiarTipoPersona_RechazaQuitarConductorConLicencia()
    {
        var persona = CrearConductor();
        persona.RegistrarLicencia(PdfValido, Ahora.AddMonths(1), Ahora);

        var act = () => persona.CambiarTipoPersona(TipoPersona.Administrativo, Ahora);

        act.Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("persona.licencia.tipo_persona");
    }
}
