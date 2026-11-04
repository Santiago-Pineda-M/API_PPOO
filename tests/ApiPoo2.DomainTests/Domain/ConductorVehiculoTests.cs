using ApiPoo2.Domain.Common;
using ApiPoo2.Domain.Personas;
using ApiPoo2.Domain.TiposDocumento;
using FluentAssertions;

namespace ApiPoo2.DomainTests.Domain;

public sealed class ConductorVehiculoRelacionTests
{
    private static readonly DateTime Ahora = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static ConductorVehiculo Crear(EstadoConductor estado = EstadoConductor.Ea)
        => ConductorVehiculo.Crear(Guid.NewGuid(), Guid.NewGuid(), Ahora, estado);

    [Fact]
    public void Crear_AsignaEstadoYFechaDeAsociacion()
    {
        var personaId = Guid.NewGuid();
        var vehiculoId = Guid.NewGuid();

        var relacion = ConductorVehiculo.Crear(personaId, vehiculoId, Ahora, EstadoConductor.Po);

        relacion.PersonaId.Should().Be(personaId);
        relacion.VehiculoId.Should().Be(vehiculoId);
        relacion.FechaAsociacion.Should().Be(Ahora);
        relacion.Estado.Should().Be(EstadoConductor.Po);
        relacion.PuedeOperar().Should().BeTrue();
    }

    [Fact]
    public void Crear_RechazaPersonaVacia()
        => FluentActions.Invoking(() => ConductorVehiculo.Crear(Guid.Empty, Guid.NewGuid(), Ahora))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("conductor.persona");

    [Fact]
    public void Crear_RechazaVehiculoVacio()
        => FluentActions.Invoking(() => ConductorVehiculo.Crear(Guid.NewGuid(), Guid.Empty, Ahora))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("conductor.vehiculo");

    [Fact]
    public void CambiarEstado_ActualizaElEstadoYLaFecha()
    {
        var relacion = Crear();

        relacion.CambiarEstado(EstadoConductor.Po, Ahora.AddMinutes(1));

        relacion.Estado.Should().Be(EstadoConductor.Po);
        relacion.UpdatedAtUtc.Should().Be(Ahora.AddMinutes(1));
    }

    [Fact]
    public void CambiarEstado_MismoEstado_NoActualizaLaFecha()
    {
        var relacion = Crear(EstadoConductor.Ea);

        relacion.CambiarEstado(EstadoConductor.Ea, Ahora.AddMinutes(1));

        relacion.UpdatedAtUtc.Should().BeNull();
    }

    [Fact]
    public void Restringir_DejaElConductorEnRo()
    {
        var relacion = Crear(EstadoConductor.Po);

        relacion.Restringir(Ahora.AddDays(1));

        relacion.Estado.Should().Be(EstadoConductor.Ro);
        relacion.PuedeOperar().Should().BeFalse();
        relacion.UpdatedAtUtc.Should().Be(Ahora.AddDays(1));
    }

    [Fact]
    public void Restringir_YaRestringido_NoActualizaLaFecha()
    {
        var relacion = Crear(EstadoConductor.Ro);

        relacion.Restringir(Ahora.AddDays(1));

        relacion.UpdatedAtUtc.Should().BeNull();
    }
}

public sealed class DocumentoVehiculoVencimientoTests
{
    private static readonly DateTime Ahora = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static DocumentoVehiculo Crear(DateTime? vencimiento = null)
        => DocumentoVehiculo.Crear(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "%PDF-1.7 contenido"u8.ToArray(),
            "SOAT.pdf",
            Ahora.AddDays(-10),
            vencimiento ?? Ahora.AddDays(30),
            Ahora);

    [Fact]
    public void MarcarVencido_PersisteElEstadoCuandoLaFechaYaPaso()
    {
        var documento = Crear(Ahora.AddDays(-1));

        documento.MarcarVencido(Ahora);

        documento.Estado.Should().Be(EstadoDocumento.Vencido);
        documento.UpdatedAtUtc.Should().Be(Ahora);
    }

    [Fact]
    public void MarcarVencido_RechazaUnDocumentoVigente()
    {
        var documento = Crear(Ahora.AddDays(30));

        var act = () => documento.MarcarVencido(Ahora);

        act.Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("vehiculo_documento.vigente");
    }

    [Fact]
    public void MarcarVencido_YaVencido_NoActualizaLaFecha()
    {
        var documento = Crear(Ahora.AddDays(-1));
        documento.MarcarVencido(Ahora);

        documento.MarcarVencido(Ahora.AddDays(1));

        documento.UpdatedAtUtc.Should().Be(Ahora);
    }

    [Fact]
    public void EstaHabilitado_RequiereEstadoHabilitadoYFechaVigente()
    {
        var documento = Crear(Ahora.AddDays(30));

        documento.EstaHabilitado(Ahora).Should().BeFalse();

        documento.CambiarEstado(EstadoDocumento.Habilitado, Ahora);
        documento.EstaHabilitado(Ahora).Should().BeTrue();
        documento.EstaHabilitado(Ahora.AddDays(60)).Should().BeFalse();
    }
}
