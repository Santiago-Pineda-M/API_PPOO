using ApiPoo2.Domain.Common;
using ApiPoo2.Domain.Vehiculos;

namespace ApiPoo2.Domain.Vehiculos;

/// <summary>
///     Obligatoriedad del documento según el tipo de vehículo, según el enunciado:
///     RA obligatorio para automóvil, RM para motocicleta, RR requerido para ambos.
/// </summary>
public sealed class CodigoObligatoriedad
{
    public const int Length = 2;

    public string Value { get; }

    private CodigoObligatoriedad(string value) => Value = value;

    public static CodigoObligatoriedad From(string value)
    {
        var normalizado = value?.Trim().ToUpperInvariant() ?? string.Empty;

        if (normalizado is not ("RA" or "RM" or "RR"))
        {
            throw new DomainValidationException(
                "documento.obligatoriedad",
                ["La obligatoriedad debe ser RA (automóvil), RM (motocicleta) o RR (ambos)."]);
        }

        return new CodigoObligatoriedad(normalizado);
    }

    /// <summary>Indica si el documento es obligatorio para el tipo de vehículo indicado.</summary>
    public bool EsObligatorioPara(TipoVehiculo tipoVehiculo) => Value switch
    {
        "RA" => tipoVehiculo == TipoVehiculo.Automovil,
        "RM" => tipoVehiculo == TipoVehiculo.Motocicleta,
        "RR" => true,
        _ => false,
    };

    public bool Equals(CodigoObligatoriedad? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => obj is CodigoObligatoriedad otro && Equals(otro);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public static bool operator ==(CodigoObligatoriedad? left, CodigoObligatoriedad? right)
        => left is null ? right is null : left.Equals(right);

    public static bool operator !=(CodigoObligatoriedad? left, CodigoObligatoriedad? right)
        => !(left == right);

    public override string ToString() => Value;
}