using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace ApiPoo2.ArchitectureTests;

public sealed partial class TestNamingTests
{
    private static readonly string DirectorioDeTests = Path.Combine(
        RaizDelRepositorio(), "tests");

    [GeneratedRegex(@"\[(?:Fact|Theory)\](?:\s*\[[^\]]*\])*\s*public\s+(?:async\s+)?(?:Task|void)\s+(\w+)\s*\(")]
    private static partial Regex MetodoDePrueba();

    [GeneratedRegex(@"^[\p{L}][\p{L}\p{N}]*(_[\p{L}][\p{L}\p{N}]*)*$")]
    private static partial Regex PatronDeNombre();

    [Fact]
    public void NombresDeTest_CumplenElEstandar()
    {
        var violaciones = ArchivosDePrueba()
            .SelectMany(NombresDeMetodos)
            .Where(nombre => !EsNombreValido(nombre))
            .ToList();

        violaciones.Should().BeEmpty();
    }

    private static IEnumerable<string> ArchivosDePrueba()
        => Directory
            .EnumerateFiles(DirectorioDeTests, "*.cs", SearchOption.AllDirectories)
            .Where(ruta => !ruta.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                         && !ruta.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Select(File.ReadAllText);

    private static IEnumerable<string> NombresDeMetodos(string codigo)
        => MetodoDePrueba()
            .Matches(codigo)
            .Select(match => match.Groups[1].Value);

    private static bool EsNombreValido(string nombre)
        => PatronDeNombre().IsMatch(nombre) && !nombre.Split('_').Contains("Should");

    private static string RaizDelRepositorio()
    {
        var directorio = new DirectoryInfo(AppContext.BaseDirectory);

        while (directorio is not null)
        {
            if (File.Exists(Path.Combine(directorio.FullName, "ApiPoo2.sln")))
            {
                return directorio.FullName;
            }

            directorio = directorio.Parent;
        }

        return AppContext.BaseDirectory;
    }
}
