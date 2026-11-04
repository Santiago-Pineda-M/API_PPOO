using ApiPoo2.Domain.Common;
using ApiPoo2.Domain.Trayectos;
using FluentAssertions;

namespace ApiPoo2.DomainTests.Domain;

public sealed class TrayectoTemporalTests
{
    private const long IdCargue = 202601011200;

    private static readonly DateTime Ahora = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static TrayectoTemporal Crear()
        => TrayectoTemporal.Crear(
            IdCargue,
            "R-001",
            "1234567890",
            "ABC123",
            "Terminal de Ibagué",
            "0",
            "4.4389",
            "-75.2322",
            "admin",
            Ahora);

    [Fact]
    public void Crear_NaceEnEstadoCargadoYConservaLosTextos()
    {
        var fila = Crear();

        fila.IdCargue.Should().Be(IdCargue);
        fila.CodigoRuta.Should().Be("R-001");
        fila.IdentificacionConductor.Should().Be("1234567890");
        fila.PlacaVehiculo.Should().Be("ABC123");
        fila.Ubicacion.Should().Be("Terminal de Ibagué");
        fila.OrdenParada.Should().Be("0");
        fila.Latitud.Should().Be("4.4389");
        fila.Longitud.Should().Be("-75.2322");
        fila.LoginUsuario.Should().Be("admin");
        fila.Estado.Should().Be(EstadoTrayectoTemporal.Cargado);
        fila.Observacion.Should().BeNull();
        fila.CreatedAtUtc.Should().Be(Ahora);
        fila.PuedeProcesarse().Should().BeFalse();
    }

    [Fact]
    public void Crear_RechazaIdentificadorNoPositivo()
    {
        var act = () => TrayectoTemporal.Crear(
            0, "R-001", "1", "ABC123", "Terminal", "0", null, null, "admin", Ahora);

        act.Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("cargue.identificador");
    }

    [Fact]
    public void MarcarValidado_AsignaPersonaYVehiculo()
    {
        var fila = Crear();
        var personaId = Guid.NewGuid();
        var vehiculoId = Guid.NewGuid();

        fila.MarcarValidado(personaId, vehiculoId, Ahora.AddMinutes(1));

        fila.Estado.Should().Be(EstadoTrayectoTemporal.Validado);
        fila.PersonaId.Should().Be(personaId);
        fila.VehiculoId.Should().Be(vehiculoId);
        fila.PuedeProcesarse().Should().BeTrue();
    }

    [Fact]
    public void MarcarValidado_RechazaUnaFilaYaProcesada()
    {
        var fila = Crear();
        fila.MarcarValidado(Guid.NewGuid(), Guid.NewGuid(), Ahora);
        fila.MarcarProcesado(Ahora);

        var act = () => fila.MarcarValidado(Guid.NewGuid(), Guid.NewGuid(), Ahora);

        act.Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("cargue.estado");
    }

    [Fact]
    public void MarcarError_AcumulaObservaciones()
    {
        var fila = Crear();

        fila.MarcarError("El conductor no existe.", Ahora.AddMinutes(1));
        fila.MarcarError("La placa es obligatoria.", Ahora.AddMinutes(2));

        fila.Estado.Should().Be(EstadoTrayectoTemporal.Error);
        fila.Observacion.Should().Be("El conductor no existe.\nLa placa es obligatoria.");
    }

    [Fact]
    public void MarcarError_RechazaObservacionVacia()
    {
        var fila = Crear();

        var act = () => fila.MarcarError("   ", Ahora);

        act.Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("cargue.observacion");
    }

    [Fact]
    public void MarcarProcesado_SoloEsValidoDesdeValidado()
    {
        var fila = Crear();

        var act = () => fila.MarcarProcesado(Ahora);

        act.Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("cargue.estado");
    }

    [Fact]
    public void MarcarProcesado_CambiaElEstadoYActualizaLaFecha()
    {
        var fila = Crear();
        fila.MarcarValidado(Guid.NewGuid(), Guid.NewGuid(), Ahora);

        fila.MarcarProcesado(Ahora.AddMinutes(3));

        fila.Estado.Should().Be(EstadoTrayectoTemporal.Procesado);
        fila.PuedeProcesarse().Should().BeFalse();
        fila.UpdatedAtUtc.Should().Be(Ahora.AddMinutes(3));
    }
}
