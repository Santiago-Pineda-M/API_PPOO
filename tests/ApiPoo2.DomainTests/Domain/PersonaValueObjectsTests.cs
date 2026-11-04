using ApiPoo2.Domain.Common;
using ApiPoo2.Domain.Personas;
using FluentAssertions;

namespace ApiPoo2.DomainTests.Domain;

public sealed class NombresTests
{
    [Fact]
    public void From_RecortaEspacios()
        => Nombres.From("  Juan Carlos  ").Value.Should().Be("Juan Carlos");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void From_RechazaVacio(string? valor)
        => FluentActions.Invoking(() => Nombres.From(valor!))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("persona.nombres");

    [Fact]
    public void From_RechazaSuperaMaxLength()
        => FluentActions.Invoking(() => Nombres.From(new string('a', Nombres.MaxLength + 1)))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("persona.nombres");

    [Fact]
    public void From_RechazaSinLetras()
        => FluentActions.Invoking(() => Nombres.From("12345"))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("persona.nombres");

    [Fact]
    public void From_AceptaLongitudExactaMaxima()
        => Nombres.From(new string('a', Nombres.MaxLength)).Value.Length.Should().Be(Nombres.MaxLength);

    [Fact]
    public void Iguales_ComparanPorValor()
    {
        var a = Nombres.From("Ana");
        var b = Nombres.From("Ana");
        var c = Nombres.From("Bea");

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
        a.Should().NotBe(c);
        a.Should().NotBe((object)"Ana");
        a.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void Operadores_ComparanNulos()
    {
        Nombres? a = Nombres.From("Ana");
        Nombres? nulo = null;

        (a == Nombres.From("Ana")).Should().BeTrue();
        (a != Nombres.From("Bea")).Should().BeTrue();
        (nulo == null).Should().BeTrue();
        (a == null).Should().BeFalse();
        (a != null).Should().BeTrue();
    }

    [Fact]
    public void ToString_DevuelveElValor()
        => Nombres.From("Ana").ToString().Should().Be("Ana");
}

public sealed class ApellidosTests
{
    [Fact]
    public void From_RecortaEspacios()
        => Apellidos.From("  Pérez Gómez  ").Value.Should().Be("Pérez Gómez");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void From_RechazaVacio(string? valor)
        => FluentActions.Invoking(() => Apellidos.From(valor!))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("persona.apellidos");

    [Fact]
    public void From_RechazaSuperaMaxLength()
        => FluentActions.Invoking(() => Apellidos.From(new string('a', Apellidos.MaxLength + 1)))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("persona.apellidos");

    [Fact]
    public void From_RechazaSinLetras()
        => FluentActions.Invoking(() => Apellidos.From("12345"))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("persona.apellidos");

    [Fact]
    public void Iguales_ComparanPorValor()
    {
        var a = Apellidos.From("Pérez");
        var b = Apellidos.From("Pérez");

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
        a.Should().NotBe(Apellidos.From("Gómez"));
        a.Should().NotBe((object)"Pérez");
        a.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void Operadores_ComparanNulos()
    {
        Apellidos? a = Apellidos.From("Pérez");
        Apellidos? nulo = null;

        (a == Apellidos.From("Pérez")).Should().BeTrue();
        (a != Apellidos.From("Gómez")).Should().BeTrue();
        (nulo == null).Should().BeTrue();
        (a == null).Should().BeFalse();
        (a != null).Should().BeTrue();
    }

    [Fact]
    public void ToString_DevuelveElValor()
        => Apellidos.From("Pérez").ToString().Should().Be("Pérez");
}

public sealed class NumeroIdentificacionTests
{
    [Fact]
    public void From_AceptaSoloDigitos()
        => NumeroIdentificacion.From("1234567890").Value.Should().Be("1234567890");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void From_RechazaVacio(string? valor)
        => FluentActions.Invoking(() => NumeroIdentificacion.From(valor!))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("persona.identificacion");

    [Fact]
    public void From_RechazaSuperaMaxLength()
        => FluentActions.Invoking(() => NumeroIdentificacion.From(new string('1', NumeroIdentificacion.MaxLength + 1)))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("persona.identificacion");

    [Theory]
    [InlineData("12A456")]
    [InlineData("-1234")]
    [InlineData("12 34")]
    public void From_RechazaNoNumerico(string valor)
        => FluentActions.Invoking(() => NumeroIdentificacion.From(valor))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("persona.identificacion");

    [Fact]
    public void From_ConservaLosCerosIniciales()
        => NumeroIdentificacion.From("0012").Value.Should().Be("0012");

    [Theory]
    [InlineData(" 0012")]
    [InlineData("0012 ")]
    [InlineData("00 12")]
    public void From_RechazaEspacios(string valor)
        => FluentActions.Invoking(() => NumeroIdentificacion.From(valor))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("persona.identificacion");

    [Fact]
    public void Iguales_ComparanPorValor()
    {
        var a = NumeroIdentificacion.From("123");
        var b = NumeroIdentificacion.From("123");

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
        a.Should().NotBe(NumeroIdentificacion.From("456"));
        a.Should().NotBe((object)"123");
        a.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void Operadores_ComparanNulos()
    {
        NumeroIdentificacion? a = NumeroIdentificacion.From("123");
        NumeroIdentificacion? nulo = null;

        (a == NumeroIdentificacion.From("123")).Should().BeTrue();
        (a != NumeroIdentificacion.From("456")).Should().BeTrue();
        (nulo == null).Should().BeTrue();
        (a == null).Should().BeFalse();
        (a != null).Should().BeTrue();
    }

    [Fact]
    public void ToString_DevuelveElValor()
        => NumeroIdentificacion.From("123").ToString().Should().Be("123");
}

public sealed class CorreoElectronicoTests
{
    [Theory]
    [InlineData("  Juan.Perez@Example.COM  ", "juan.perez@example.com")]
    [InlineData("a@b.co", "a@b.co")]
    public void From_NormalizaYDerecha(string entrada, string esperado)
        => CorreoElectronico.From(entrada).Value.Should().Be(esperado);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void From_RechazaVacio(string? valor)
        => FluentActions.Invoking(() => CorreoElectronico.From(valor!))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("persona.correo");

    [Fact]
    public void From_RechazaSuperaMaxLength()
    {
        var local = new string('a', 64);
        var domain = new string('b', 300);

        FluentActions.Invoking(() => CorreoElectronico.From(local + "@" + domain))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("persona.correo");
    }

    [Theory]
    [InlineData("sin-arroba.com")]      // no tiene @
    [InlineData("dos@arroba@com")]      // más de un @
    [InlineData("@example.com")]        // parte local vacía
    [InlineData("usuario@")]            // dominio vacío
    [InlineData("usuario@sindominio")]  // dominio sin punto
    [InlineData("usuario@.com")]        // dominio empieza en punto
    [InlineData("usuario@example.")]    // dominio termina en punto
    public void From_RechazaFormatoInvalido(string valor)
        => FluentActions.Invoking(() => CorreoElectronico.From(valor))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("persona.correo");

    [Fact]
    public void From_RechazaParteLocalLarga()
    {
        var local = new string('a', 65);

        FluentActions.Invoking(() => CorreoElectronico.From(local + "@example.com"))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("persona.correo");
    }

    [Fact]
    public void From_RechazaDominioLargo()
    {
        var dominio = new string('a', 252) + ".com";

        FluentActions.Invoking(() => CorreoElectronico.From("u@" + dominio))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("persona.correo");
    }

    [Fact]
    public void Iguales_ComparanPorValor()
    {
        var a = CorreoElectronico.From("juan@example.com");
        var b = CorreoElectronico.From("JUAN@example.com");

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
        a.Should().NotBe(CorreoElectronico.From("otro@example.com"));
        a.Should().NotBe((object)"juan@example.com");
        a.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void Operadores_ComparanNulos()
    {
        CorreoElectronico? a = CorreoElectronico.From("juan@example.com");
        CorreoElectronico? nulo = null;

        (a == CorreoElectronico.From("juan@example.com")).Should().BeTrue();
        (a != CorreoElectronico.From("otro@example.com")).Should().BeTrue();
        (nulo == null).Should().BeTrue();
        (a == null).Should().BeFalse();
        (a != null).Should().BeTrue();
    }

    [Fact]
    public void ToString_DevuelveElValor()
        => CorreoElectronico.From("juan@example.com").ToString().Should().Be("juan@example.com");
}

public sealed class ApiKeyTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void From_RechazaVacio(string? valor)
        => FluentActions.Invoking(() => ApiKey.From(valor!))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("user.apikey");

    [Fact]
    public void Generate_ProduceClaveUnicaYNoVacia()
    {
        var primera = ApiKey.Generate();
        var segunda = ApiKey.Generate();

        primera.Value.Should().NotBeNullOrWhiteSpace();
        primera.Value.Length.Should().Be(ApiKey.Length * 2);
        primera.Should().NotBe(segunda);
    }

    [Fact]
    public void From_AceptaValoresPreexistentes()
        => ApiKey.From("ABC123").Value.Should().Be("ABC123");

    [Fact]
    public void Iguales_ComparanPorValor()
    {
        var a = ApiKey.From("ABC123");
        var b = ApiKey.From("ABC123");

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
        a.Should().NotBe(ApiKey.From("XYZ789"));
        a.Should().NotBe((object)"ABC123");
        a.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void Operadores_ComparanNulos()
    {
        ApiKey? a = ApiKey.From("ABC123");
        ApiKey? nulo = null;

        (a == ApiKey.From("ABC123")).Should().BeTrue();
        (a != ApiKey.From("XYZ789")).Should().BeTrue();
        (nulo == null).Should().BeTrue();
        (a == null).Should().BeFalse();
        (a != null).Should().BeTrue();
    }

    [Fact]
    public void ToString_DevuelveElValor()
        => ApiKey.From("ABC123").ToString().Should().Be("ABC123");
}

public sealed class LoginValueObjectTests
{
    [Fact]
    public void From_RechazaVacio()
        => FluentActions.Invoking(() => Login.From(null!))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("user.login");

    [Fact]
    public void From_RechazaSuperaMaxLength()
        => FluentActions.Invoking(() => Login.From(new string('a', Login.MaxLength + 1)))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("user.login");

    [Theory]
    [InlineData("juan perez")]
    [InlineData("juan@correo")]
    [InlineData("juan-perez")]
    public void From_RechazaCaracteresNoPermitidos(string valor)
        => FluentActions.Invoking(() => Login.From(valor))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("user.login");

    [Fact]
    public void From_AceptaLongitudExactaMaxima()
        => Login.From(new string('a', Login.MaxLength)).Value.Length.Should().Be(Login.MaxLength);

    [Fact]
    public void FromMnemonic_SinLetrasInicialesQuedanVacias()
        => Login.FromMnemonic("123", "456", NumeroIdentificacion.From("789")).Value.Should().Be("789");

    [Fact]
    public void FromMnemonic_BuscaLaPrimeraLetraNoLaPrimeraPosicion()
        => Login.FromMnemonic("  Juan", "- Pérez", NumeroIdentificacion.From("1")).Value.Should().Be("JP1");

    [Fact]
    public void Iguales_ComparanPorValor()
    {
        var a = Login.From("JP123");
        var b = Login.From("JP123");

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
        a.Should().NotBe(Login.From("JP124"));
        a.Should().NotBe((object)"JP123");
        a.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void Operadores_ComparanNulos()
    {
        Login? a = Login.From("JP123");
        Login? nulo = null;

        (a == Login.From("JP123")).Should().BeTrue();
        (a != Login.From("JP124")).Should().BeTrue();
        (nulo == null).Should().BeTrue();
        (a == null).Should().BeFalse();
        (a != null).Should().BeTrue();
    }

    [Fact]
    public void ToString_DevuelveElValor()
        => Login.From("JP123").ToString().Should().Be("JP123");
}
