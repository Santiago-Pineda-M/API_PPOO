using ApiPoo2.Domain.Common;
using ApiPoo2.Domain.Personas;
using FluentAssertions;

namespace ApiPoo2.UnitTests.Domain;

public sealed class PersonaTests
{
    private static readonly DateTime Ahora = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static Persona CrearAdministrativo(string numero = "1234567890", string nombres = "Juan", string apellidos = "Pérez")
        => Persona.Register(TipoIdentificacion.CedulaCiudadania, numero, nombres, apellidos, "test@example.com", TipoPersona.Administrativo, Ahora);

    private static Persona CrearConductor()
        => Persona.Register(
            TipoIdentificacion.CedulaCiudadania,
            "9876543210",
            "Ana",
            "Gómez",
            "ana.gomez@example.com",
            TipoPersona.Conductor,
            Ahora);

    [Fact]
    public void Register_AsignaIdentidad()
    {
        var persona = CrearAdministrativo();

        persona.Id.Should().NotBe(Guid.Empty);
        persona.CreatedAtUtc.Should().Be(Ahora);
    }

    [Fact]
    public void Register_RechazaIdentificacionNoNumerica()
        => FluentActions.Invoking(() => CrearAdministrativo("ABC123"))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("persona.identificacion");

    [Fact]
    public void Register_RechazaNombresVacios()
        => FluentActions.Invoking(() => Persona.Register(
                TipoIdentificacion.CedulaCiudadania, "123", "  ", "Pérez", "test@test.com", TipoPersona.Administrativo, Ahora))
            .Should().Throw<DomainValidationException>();

    [Fact]
    public void ActualizarDatos_CambiaNombresApellidosYCorreo()
    {
        var persona = CrearAdministrativo();

        persona.ActualizarDatos("Juan Carlos", "Pérez Gómez", "juan.carlos@example.com", Ahora);

        persona.Nombres.Value.Should().Be("Juan Carlos");
        persona.Apellidos.Value.Should().Be("Pérez Gómez");
        persona.CorreoElectronico.Value.Should().Be("juan.carlos@example.com");
        persona.UpdatedAtUtc.Should().Be(Ahora);
    }

    [Fact]
    public void SoloElAdministrativoPuedeTenerUsuario()
    {
        CrearAdministrativo().PuedeTenerUsuario().Should().BeTrue();
        CrearConductor().PuedeTenerUsuario().Should().BeFalse();
    }

    [Fact]
    public void CrearUsuario_GeneraLoginMnemonicoYApiKey()
    {
        var persona = CrearAdministrativo();

        var user = persona.CrearUsuario("hash", Ahora);

        user.Login.Value.Should().Be("JP1234567890");
        user.ApiKey.Value.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void CrearUsuario_RechazaPersonaConductor()
        => FluentActions.Invoking(() => CrearConductor().CrearUsuario("hash", Ahora))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("persona.usuario.no_admitido");

    [Fact]
    public void CrearUsuario_RechazaSegundoUsuario()
    {
        var persona = CrearAdministrativo();
        persona.CrearUsuario("hash", Ahora);

        FluentActions.Invoking(() => persona.CrearUsuario("otro", Ahora))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("persona.usuario.duplicado");
    }

    [Fact]
    public void CrearUsuario_AplicaSufijoParaHomonimos()
    {
        var persona = CrearAdministrativo();
        persona.CrearUsuario("hash", Ahora, sufijoLogin: 1);

        persona.Usuario!.Login.Value.Should().Be("JP1234567890_1");
    }

    [Fact]
    public void CambiarTipoPersona_NoQuitaAdministrativoSiTieneUsuario()
    {
        var persona = CrearAdministrativo();
        persona.CrearUsuario("hash", Ahora);

        FluentActions.Invoking(() => persona.CambiarTipoPersona(TipoPersona.Conductor, Ahora))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("persona.usuario.requerido");
    }

    [Fact]
    public void CambiarTipoPersona_PermiteQuitarAdministrativoSinUsuario()
    {
        var persona = CrearAdministrativo();

        persona.CambiarTipoPersona(TipoPersona.Conductor, Ahora);

        persona.TipoPersona.Should().Be(TipoPersona.Conductor);
    }
}

public sealed class LoginTests
{
    [Theory]
    [InlineData("Juan", "Pérez", "1234567890", 0, "JP1234567890")]
    [InlineData("juan", "pérez", "1234567890", 0, "JP1234567890")]
    [InlineData("Ana María", "Gómez", "555", 0, "AG555")]
    [InlineData("Juan", "Pérez", "1234567890", 1, "JP1234567890_1")]
    [InlineData("Juan", "Pérez", "1234567890", 2, "JP1234567890_2")]
    public void FromMnemonic_AplicaLaReglaDelEnunciado(
        string nombres, string apellidos, string numero, int sufijo, string esperado)
    {
        var numero_ = NumeroIdentificacion.From(numero);

        Login.FromMnemonic(nombres, apellidos, numero_, sufijo).Value.Should().Be(esperado);
    }

    [Fact]
    public void From_RechazaCaracteresInvalidos()
        => FluentActions.Invoking(() => Login.From("jp 123!"))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("user.login");

    [Fact]
    public void From_RechazaVacio()
        => FluentActions.Invoking(() => Login.From("  "))
            .Should().Throw<DomainValidationException>();

    [Fact]
    public void From_AceptaGuionBajoParaDesambiguar()
        => Login.From("jp123_1").Value.Should().Be("jp123_1");
}

public sealed class ConductorVehiculoTests
{
    private static readonly DateTime Ahora = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Crear_NaceEnEsperaDeAprobacion()
    {
        var relacion = ConductorVehiculo.Crear(Guid.NewGuid(), Guid.NewGuid(), Ahora);

        relacion.Estado.Should().Be(EstadoConductor.Ea);
        relacion.PuedeOperar().Should().BeFalse();
        relacion.FechaAsociacion.Should().Be(Ahora);
    }

    [Fact]
    public void Crear_RechazaIdentificadoresVacios()
    {
        FluentActions.Invoking(() => ConductorVehiculo.Crear(Guid.Empty, Guid.NewGuid(), Ahora))
            .Should().Throw<DomainValidationException>();
        FluentActions.Invoking(() => ConductorVehiculo.Crear(Guid.NewGuid(), Guid.Empty, Ahora))
            .Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void CambiarEstado_AplicaPo()
    {
        var relacion = ConductorVehiculo.Crear(Guid.NewGuid(), Guid.NewGuid(), Ahora);

        relacion.CambiarEstado(EstadoConductor.Po, Ahora);

        relacion.Estado.Should().Be(EstadoConductor.Po);
        relacion.PuedeOperar().Should().BeTrue();
    }

    [Fact]
    public void CambiarEstado_AplicaRestriccionOperar()
    {
        var relacion = ConductorVehiculo.Crear(Guid.NewGuid(), Guid.NewGuid(), Ahora, EstadoConductor.Po);

        relacion.CambiarEstado(EstadoConductor.Ro, Ahora);

        relacion.PuedeOperar().Should().BeFalse();
    }
}