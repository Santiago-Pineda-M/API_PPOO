using ApiPoo2.Domain.Common;
using ApiPoo2.Domain.TiposDocumento;
using FluentAssertions;

namespace ApiPoo2.DomainTests.Domain;

public sealed class TipoDocumentoCodigoTests
{
    [Theory]
    [InlineData("  soat  ", "SOAT")]
    [InlineData("rt_2026", "RT_2026")]
    public void From_NormalizaAMayusculas(string entrada, string esperado)
        => TipoDocumentoCodigo.From(entrada).Value.Should().Be(esperado);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void From_RechazaVacio(string? valor)
        => FluentActions.Invoking(() => TipoDocumentoCodigo.From(valor!))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("documento.codigo");

    [Fact]
    public void From_RechazaSuperaMaxLength()
        => FluentActions.Invoking(() => TipoDocumentoCodigo.From(new string('a', TipoDocumentoCodigo.MaxLength + 1)))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("documento.codigo");

    [Theory]
    [InlineData("SOAT-2026")]
    [InlineData("SOAT 2026")]
    [InlineData("SOAT/2026")]
    public void From_RechazaCaracteresNoPermitidos(string valor)
        => FluentActions.Invoking(() => TipoDocumentoCodigo.From(valor))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("documento.codigo");

    [Fact]
    public void From_AceptaLongitudExactaMaxima()
        => TipoDocumentoCodigo.From(new string('A', TipoDocumentoCodigo.MaxLength)).Value
            .Length.Should().Be(TipoDocumentoCodigo.MaxLength);

    [Fact]
    public void Iguales_ComparanPorValor()
    {
        var a = TipoDocumentoCodigo.From("SOAT");
        var b = TipoDocumentoCodigo.From("soat");

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
        a.Should().NotBe(TipoDocumentoCodigo.From("RT"));
        a.Should().NotBe((object)"SOAT");
        a.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void Operadores_ComparanNulos()
    {
        TipoDocumentoCodigo? a = TipoDocumentoCodigo.From("SOAT");
        TipoDocumentoCodigo? nulo = null;

        (a == TipoDocumentoCodigo.From("SOAT")).Should().BeTrue();
        (a != TipoDocumentoCodigo.From("RT")).Should().BeTrue();
        (nulo == null).Should().BeTrue();
        (a == null).Should().BeFalse();
        (a != null).Should().BeTrue();
    }

    [Fact]
    public void ToString_DevuelveElValor()
        => TipoDocumentoCodigo.From("SOAT").ToString().Should().Be("SOAT");
}

public sealed class TipoDocumentoNombreTests
{
    [Fact]
    public void From_RecortaEspacios()
        => TipoDocumentoNombre.From("  Seguro Obligatorio  ").Value.Should().Be("Seguro Obligatorio");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void From_RechazaVacio(string? valor)
        => FluentActions.Invoking(() => TipoDocumentoNombre.From(valor!))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("documento.nombre");

    [Fact]
    public void From_RechazaSuperaMaxLength()
        => FluentActions.Invoking(() => TipoDocumentoNombre.From(new string('a', TipoDocumentoNombre.MaxLength + 1)))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("documento.nombre");

    [Fact]
    public void From_AceptaLongitudExactaMaxima()
        => TipoDocumentoNombre.From(new string('a', TipoDocumentoNombre.MaxLength)).Value
            .Length.Should().Be(TipoDocumentoNombre.MaxLength);

    [Fact]
    public void Iguales_ComparanPorValor()
    {
        var a = TipoDocumentoNombre.From("SOAT");
        var b = TipoDocumentoNombre.From("SOAT");

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
        a.Should().NotBe(TipoDocumentoNombre.From("RT"));
        a.Should().NotBe((object)"SOAT");
        a.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void Operadores_ComparanNulos()
    {
        TipoDocumentoNombre? a = TipoDocumentoNombre.From("SOAT");
        TipoDocumentoNombre? nulo = null;

        (a == TipoDocumentoNombre.From("SOAT")).Should().BeTrue();
        (a != TipoDocumentoNombre.From("RT")).Should().BeTrue();
        (nulo == null).Should().BeTrue();
        (a == null).Should().BeFalse();
        (a != null).Should().BeTrue();
    }

    [Fact]
    public void ToString_DevuelveElValor()
        => TipoDocumentoNombre.From("SOAT").ToString().Should().Be("SOAT");
}

public sealed class NombreArchivoTests
{
    [Fact]
    public void From_RecortaEspacios()
        => NombreArchivo.From("  SOAT.pdf  ").Value.Should().Be("SOAT.pdf");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void From_RechazaVacio(string? valor)
        => FluentActions.Invoking(() => NombreArchivo.From(valor!))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("documento.archivo");

    [Fact]
    public void From_RechazaSuperaMaxLength()
        => FluentActions.Invoking(() => NombreArchivo.From(new string('a', NombreArchivo.MaxLength + 1)))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("documento.archivo");

    [Fact]
    public void From_AceptaLongitudExactaMaxima()
        => NombreArchivo.From(new string('a', NombreArchivo.MaxLength)).Value
            .Length.Should().Be(NombreArchivo.MaxLength);

    [Fact]
    public void Iguales_ComparanPorValor()
    {
        var a = NombreArchivo.From("SOAT.pdf");
        var b = NombreArchivo.From("SOAT.pdf");

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
        a.Should().NotBe(NombreArchivo.From("RT.pdf"));
        a.Should().NotBe((object)"SOAT.pdf");
        a.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void Operadores_ComparanNulos()
    {
        NombreArchivo? a = NombreArchivo.From("SOAT.pdf");
        NombreArchivo? nulo = null;

        (a == NombreArchivo.From("SOAT.pdf")).Should().BeTrue();
        (a != NombreArchivo.From("RT.pdf")).Should().BeTrue();
        (nulo == null).Should().BeTrue();
        (a == null).Should().BeFalse();
        (a != null).Should().BeTrue();
    }

    [Fact]
    public void ToString_DevuelveElValor()
        => NombreArchivo.From("SOAT.pdf").ToString().Should().Be("SOAT.pdf");
}

public sealed class TipoDocumentoDescripcionTests
{
    [Fact]
    public void From_RecortaEspacios()
        => TipoDocumentoDescripcion.From("  Cobertura nationwide  ").Value.Should().Be("Cobertura nationwide");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void From_RechazaVacio(string? valor)
        => FluentActions.Invoking(() => TipoDocumentoDescripcion.From(valor!))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("documento.descripcion");

    [Fact]
    public void From_RechazaSuperaMaxLength()
        => FluentActions.Invoking(() => TipoDocumentoDescripcion.From(
                new string('a', TipoDocumentoDescripcion.MaxLength + 1)))
            .Should().Throw<DomainValidationException>()
            .Which.Code.Should().Be("documento.descripcion");

    [Fact]
    public void From_AceptaLongitudExactaMaxima()
        => TipoDocumentoDescripcion.From(new string('a', TipoDocumentoDescripcion.MaxLength)).Value
            .Length.Should().Be(TipoDocumentoDescripcion.MaxLength);

    [Fact]
    public void Iguales_ComparanPorValor()
    {
        var a = TipoDocumentoDescripcion.From("desc");
        var b = TipoDocumentoDescripcion.From("desc");

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
        a.Should().NotBe(TipoDocumentoDescripcion.From("otra"));
        a.Should().NotBe((object)"desc");
        a.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void Operadores_ComparanNulos()
    {
        TipoDocumentoDescripcion? a = TipoDocumentoDescripcion.From("desc");
        TipoDocumentoDescripcion? nulo = null;

        (a == TipoDocumentoDescripcion.From("desc")).Should().BeTrue();
        (a != TipoDocumentoDescripcion.From("otra")).Should().BeTrue();
        (nulo == null).Should().BeTrue();
        (a == null).Should().BeFalse();
        (a != null).Should().BeTrue();
    }

    [Fact]
    public void ToString_DevuelveElValor()
        => TipoDocumentoDescripcion.From("desc").ToString().Should().Be("desc");
}
