using ApiPoo2.Domain.Common;
using ApiPoo2.Domain.Rutas;
using FluentAssertions;

namespace ApiPoo2.DomainTests.Domain;

public sealed class RutaValueObjectsTests
{
    [Fact]
    public void CodigoRuta_NormalizaEspaciosYMayusculas()
    {
        var codigo = CodigoRuta.From("  r-001  ");

        codigo.Value.Should().Be("R-001");
        codigo.ToString().Should().Be("R-001");
        codigo.Equals(CodigoRuta.From("R-001")).Should().BeTrue();
        (codigo == CodigoRuta.From("R-001")).Should().BeTrue();
        (codigo != CodigoRuta.From("R-002")).Should().BeTrue();
        codigo.GetHashCode().Should().Be(CodigoRuta.From("R-001").GetHashCode());
    }

    [Fact]
    public void CodigoRuta_RechazaValorVacio()
        => FluentActions.Invoking(() => CodigoRuta.From("   "))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("ruta.codigo.requerido");

    [Fact]
    public void CodigoRuta_RechazaLongitudExcedida()
    {
        var largo = new string('A', CodigoRuta.MaxLength + 1);

        var act = () => CodigoRuta.From(largo);

        act.Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("ruta.codigo.longitud");
    }

    [Fact]
    public void UbicacionParada_RecortaLosEspacios()
    {
        var ubicacion = UbicacionParada.From("  Terminal de Ibagué  ");

        ubicacion.Value.Should().Be("Terminal de Ibagué");
        ubicacion.GetHashCode().Should().Be(UbicacionParada.From("Terminal de Ibagué").GetHashCode());
        ubicacion.Equals(UbicacionParada.From("Terminal de Ibagué")).Should().BeTrue();
        ubicacion.ToString().Should().Be("Terminal de Ibagué");
    }

    [Fact]
    public void UbicacionParada_RechazaValorVacio()
        => FluentActions.Invoking(() => UbicacionParada.From(""))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("parada.ubicacion.requerida");

    [Fact]
    public void UbicacionParada_RechazaLongitudExcedida()
    {
        var largo = new string('A', UbicacionParada.MaxLength + 1);

        var act = () => UbicacionParada.From(largo);

        act.Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("parada.ubicacion.longitud");
    }

    [Fact]
    public void LoginUsuario_RecortaLosEspacios()
    {
        var login = LoginUsuario.From("  admin  ");

        login.Value.Should().Be("admin");
        login.ToString().Should().Be("admin");
    }

    [Fact]
    public void LoginUsuario_RechazaValorVacio()
        => FluentActions.Invoking(() => LoginUsuario.From(" "))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("ruta.login.requerido");

    [Fact]
    public void LoginUsuario_RechazaLongitudExcedida()
    {
        var largo = new string('A', LoginUsuario.MaxLength + 1);

        var act = () => LoginUsuario.From(largo);

        act.Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("ruta.login.longitud");
    }

    [Theory]
    [InlineData(ReglasParada.OrdenInicial, true)]
    [InlineData(3, true)]
    [InlineData(ReglasParada.OrdenMaximo, true)]
    [InlineData(-1, false)]
    [InlineData(7, false)]
    public void ReglasParada_EsValido_RespetaLosLimites(int orden, bool esperado)
        => ReglasParada.EsValido(orden).Should().Be(esperado);

    [Fact]
    public void ReglasParada_EnsureValid_RechazaOrdenInvalido()
        => FluentActions.Invoking(() => ReglasParada.EnsureValid(7))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("parada.orden.rango");
}
