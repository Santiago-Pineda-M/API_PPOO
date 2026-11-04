using ApiPoo2.Domain.Common;

namespace ApiPoo2.Domain.Rutas;

/// <summary>Lugar de parada de una ruta (por ejemplo "Conservatorio del Tolima, Ibagué, Tolima").</summary>
public sealed class UbicacionParada
{
    public const int MaxLength = 200;

    public string Value { get; }

    private UbicacionParada(string value) => Value = value;

    public static UbicacionParada From(string value)
    {
        var normalizado = value?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizado))
        {
            throw new DomainValidationException(
                "parada.ubicacion.requerida",
                ["La ubicación de la parada es obligatoria."]);
        }

        if (normalizado.Length > MaxLength)
        {
            throw new DomainValidationException(
                "parada.ubicacion.longitud",
                [$"La ubicación de la parada no puede superar los {MaxLength} caracteres."]);
        }

        return new UbicacionParada(normalizado);
    }

    public bool Equals(UbicacionParada? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => obj is UbicacionParada otro && Equals(otro);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Value;
}
