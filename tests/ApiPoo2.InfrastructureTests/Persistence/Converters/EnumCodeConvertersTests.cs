using ApiPoo2.Domain.Personas;
using ApiPoo2.Domain.TiposDocumento;
using ApiPoo2.Domain.Vehiculos;
using ApiPoo2.Infrastructure.Persistence.Converters;
using FluentAssertions;

namespace ApiPoo2.InfrastructureTests.Persistence.Converters;

public sealed class EnumCodeConvertersTests
{
    [Fact]
    public void TipoIdentificacionConverter_TraduceIdaYVuelta()
    {
        var converter = new TipoIdentificacionConverter();

        converter.ConvertToProvider(TipoIdentificacion.CedulaCiudadania).Should().Be("CC");
        converter.ConvertToProvider(TipoIdentificacion.CedulaExtranjeria).Should().Be("CE");
        converter.ConvertToProvider(TipoIdentificacion.TarjetaIdentidad).Should().Be("TI");
        converter.ConvertToProvider(TipoIdentificacion.Nit).Should().Be("NIT");
        converter.ConvertFromProvider("CC").Should().Be(TipoIdentificacion.CedulaCiudadania);
    }

    [Fact]
    public void TipoPersonaConverter_TraduceIdaYVuelta()
    {
        var converter = new TipoPersonaConverter();

        converter.ConvertToProvider(TipoPersona.Administrativo).Should().Be("ADMINISTRATIVO");
        converter.ConvertToProvider(TipoPersona.Conductor).Should().Be("CONDUCTOR");
        converter.ConvertFromProvider("CONDUCTOR").Should().Be(TipoPersona.Conductor);
    }

    [Fact]
    public void EstadoConductorConverter_TraduceIdaYVuelta()
    {
        var converter = new EstadoConductorConverter();

        converter.ConvertToProvider(EstadoConductor.Po).Should().Be("PO");
        converter.ConvertToProvider(EstadoConductor.Ea).Should().Be("EA");
        converter.ConvertToProvider(EstadoConductor.Ro).Should().Be("RO");
        converter.ConvertFromProvider("PO").Should().Be(EstadoConductor.Po);
    }

    [Fact]
    public void EstadoDocumentoConverter_TraduceIdaYVuelta()
    {
        var converter = new EstadoDocumentoConverter();

        converter.ConvertToProvider(EstadoDocumento.Habilitado).Should().Be("HABILITADO");
        converter.ConvertToProvider(EstadoDocumento.Vencido).Should().Be("VENCIDO");
        converter.ConvertToProvider(EstadoDocumento.EnVerificacion).Should().Be("EN_VERIFICACION");
        converter.ConvertFromProvider("VENCIDO").Should().Be(EstadoDocumento.Vencido);
    }

    [Fact]
    public void TipoVehiculoConverter_TraduceIdaYVueltaConCodigoDelCheck()
    {
        var converter = new TipoVehiculoConverter();

        converter.ConvertToProvider(TipoVehiculo.Automovil).Should().Be("AUTOMOVIL");
        converter.ConvertToProvider(TipoVehiculo.Motocicleta).Should().Be("MOTOCIClETA");
        converter.ConvertFromProvider("MOTOCIClETA").Should().Be(TipoVehiculo.Motocicleta);
    }

    [Fact]
    public void TipoServicioConverter_TraduceIdaYVuelta()
    {
        var converter = new TipoServicioConverter();

        converter.ConvertToProvider(TipoServicio.Publico).Should().Be("PUBLICO");
        converter.ConvertToProvider(TipoServicio.Privado).Should().Be("PRIVADO");
        converter.ConvertFromProvider("PRIVADO").Should().Be(TipoServicio.Privado);
    }

    [Fact]
    public void TipoCombustibleConverter_TraduceIdaYVuelta()
    {
        var converter = new TipoCombustibleConverter();

        converter.ConvertToProvider(TipoCombustible.Gasolina).Should().Be("GASOLINA");
        converter.ConvertToProvider(TipoCombustible.Gas).Should().Be("GAS");
        converter.ConvertToProvider(TipoCombustible.Disel).Should().Be("DISEL");
        converter.ConvertFromProvider("GAS").Should().Be(TipoCombustible.Gas);
    }
}
