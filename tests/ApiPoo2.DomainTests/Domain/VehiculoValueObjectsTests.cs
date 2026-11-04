using ApiPoo2.Domain.Common;
using ApiPoo2.Domain.Vehiculos;
using FluentAssertions;

namespace ApiPoo2.DomainTests.Domain;

public sealed class MarcaTests
{
    [Fact]
    public void From_RecortaEspacios()
        => Marca.From("  Toyota  ").Value.Should().Be("Toyota");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void From_RechazaVacio(string? valor)
        => FluentActions.Invoking(() => Marca.From(valor!))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("vehiculo.marca");

    [Fact]
    public void From_RechazaSuperaMaxLength()
        => FluentActions.Invoking(() => Marca.From(new string('a', Marca.MaxLength + 1)))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("vehiculo.marca");

    [Fact]
    public void From_AceptaLongitudExactaMaxima()
        => Marca.From(new string('a', Marca.MaxLength)).Value.Length.Should().Be(Marca.MaxLength);

    [Fact]
    public void Iguales_ComparanPorValor()
    {
        var a = Marca.From("Toyota");
        var b = Marca.From("Toyota");

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
        a.Should().NotBe(Marca.From("Nissan"));
        a.Should().NotBe((object)"Toyota");
        a.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void Operadores_ComparanNulos()
    {
        Marca? a = Marca.From("Toyota");
        Marca? nulo = null;

        (a == Marca.From("Toyota")).Should().BeTrue();
        (a != Marca.From("Nissan")).Should().BeTrue();
        (nulo == null).Should().BeTrue();
        (a == null).Should().BeFalse();
        (a != null).Should().BeTrue();
    }

    [Fact]
    public void ToString_DevuelveElValor()
        => Marca.From("Toyota").ToString().Should().Be("Toyota");
}

public sealed class LineaTests
{
    [Fact]
    public void From_RecortaEspacios()
        => Linea.From("  Fortuner SW  ").Value.Should().Be("Fortuner SW");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void From_RechazaVacio(string? valor)
        => FluentActions.Invoking(() => Linea.From(valor!))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("vehiculo.linea");

    [Fact]
    public void From_RechazaSuperaMaxLength()
        => FluentActions.Invoking(() => Linea.From(new string('a', Linea.MaxLength + 1)))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("vehiculo.linea");

    [Fact]
    public void From_AceptaLongitudExactaMaxima()
        => Linea.From(new string('a', Linea.MaxLength)).Value.Length.Should().Be(Linea.MaxLength);

    [Fact]
    public void Iguales_ComparanPorValor()
    {
        var a = Linea.From("Fortuner");
        var b = Linea.From("Fortuner");

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
        a.Should().NotBe(Linea.From("Versa"));
        a.Should().NotBe((object)"Fortuner");
        a.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void Operadores_ComparanNulos()
    {
        Linea? a = Linea.From("Fortuner");
        Linea? nulo = null;

        (a == Linea.From("Fortuner")).Should().BeTrue();
        (a != Linea.From("Versa")).Should().BeTrue();
        (nulo == null).Should().BeTrue();
        (a == null).Should().BeFalse();
        (a != null).Should().BeTrue();
    }

    [Fact]
    public void ToString_DevuelveElValor()
        => Linea.From("Fortuner").ToString().Should().Be("Fortuner");
}

public sealed class ColorTests
{
    [Theory]
    [InlineData("#ff5733", "#FF5733")]
    [InlineData("  #FF5733  ", "#FF5733")]
    [InlineData("#000000", "#000000")]
    public void From_NormalizaAMayusculas(string entrada, string esperado)
        => Color.From(entrada).Value.Should().Be(esperado);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void From_RechazaVacio(string? valor)
        => FluentActions.Invoking(() => Color.From(valor!))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("vehiculo.color");

    [Theory]
    [InlineData("#FF573")]     // longitud incorrecta
    [InlineData("#FF5733FF")]  // longitud incorrecta
    public void From_RechazaLongitudDistintaDeSiete(string valor)
        => FluentActions.Invoking(() => Color.From(valor))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("vehiculo.color");

    [Theory]
    [InlineData("FF5733")]     // sin #
    [InlineData("#GG5733")]    // no hexadecimal
    [InlineData("# 5733")]
    public void From_RechazaFormatoSinAlmohadilla(string valor)
        => FluentActions.Invoking(() => Color.From(valor))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("vehiculo.color");

    [Fact]
    public void Iguales_ComparanPorValor()
    {
        var a = Color.From("#FF5733");
        var b = Color.From("#ff5733");

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
        a.Should().NotBe(Color.From("#112233"));
        a.Should().NotBe((object)"#FF5733");
        a.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void Operadores_ComparanNulos()
    {
        Color? a = Color.From("#FF5733");
        Color? nulo = null;

        (a == Color.From("#FF5733")).Should().BeTrue();
        (a != Color.From("#112233")).Should().BeTrue();
        (nulo == null).Should().BeTrue();
        (a == null).Should().BeFalse();
        (a != null).Should().BeTrue();
    }

    [Fact]
    public void ToString_DevuelveElValor()
        => Color.From("#FF5733").ToString().Should().Be("#FF5733");
}

public sealed class PlacaTests
{
    [Fact]
    public void From_NormalizaAMayusculas()
        => Placa.From("  abc123  ", TipoVehiculo.Automovil).Value.Should().Be("ABC123");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void From_RechazaVacio(string? valor)
        => FluentActions.Invoking(() => Placa.From(valor!, TipoVehiculo.Automovil))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("vehiculo.placa");

    [Fact]
    public void FromCualquiera_RechazaVacio()
        => FluentActions.Invoking(() => Placa.FromCualquiera("   "))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("vehiculo.placa");

    [Fact]
    public void FromCualquiera_RechazaLargoDistintoDeSeis()
        => FluentActions.Invoking(() => Placa.FromCualquiera("ABC1234"))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("vehiculo.placa");

    [Theory]
    [InlineData("ABC123", TipoVehiculo.Automovil, true)]
    [InlineData("ABC12D", TipoVehiculo.Automovil, false)]
    [InlineData("ABC12D", TipoVehiculo.Motocicleta, true)]
    [InlineData("ABC123", TipoVehiculo.Motocicleta, false)]
    public void EsValidaPara_DistingueLosDosFormatos(string placa, TipoVehiculo tipo, bool esperado)
        => Placa.FromCualquiera(placa).EsValidaPara(tipo).Should().Be(esperado);

    [Fact]
    public void Iguales_ComparanPorValor()
    {
        var a = Placa.From("ABC123", TipoVehiculo.Automovil);
        var b = Placa.From("abc123", TipoVehiculo.Automovil);

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
        a.Should().NotBe(Placa.From("XYZ789", TipoVehiculo.Automovil));
        a.Should().NotBe((object)"ABC123");
        a.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void Operadores_ComparanNulos()
    {
        Placa? a = Placa.From("ABC123", TipoVehiculo.Automovil);
        Placa? nulo = null;

        (a == Placa.From("ABC123", TipoVehiculo.Automovil)).Should().BeTrue();
        (a != Placa.From("XYZ789", TipoVehiculo.Automovil)).Should().BeTrue();
        (nulo == null).Should().BeTrue();
        (a == null).Should().BeFalse();
        (a != null).Should().BeTrue();
    }

    [Fact]
    public void ToString_DevuelveElValor()
        => Placa.From("ABC123", TipoVehiculo.Automovil).ToString().Should().Be("ABC123");
}
