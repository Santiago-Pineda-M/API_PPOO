using ApiPoo2.Domain.Common;

namespace ApiPoo2.Domain.Vehiculos;

/// <summary>
///     Tipos de vehículo a los que aplica un documento paramétrico, según el enunciado:
///     A para automóvil, M para motocicleta, AM para ambos.
/// </summary>
public sealed class TiposVehiculoAplicables
{
    public const int MaxLength = 2;

    public string Value { get; }

    private TiposVehiculoAplicables(string value) => Value = value;

    public static TiposVehiculoAplicables From(string value)
    {
        var normalizado = value?.Trim().ToUpperInvariant() ?? string.Empty;

        if (normalizado is not ("A" or "M" or "AM"))
        {
            throw new DomainValidationException(
                "documento.tipos_aplicables",
                ["Los tipos de vehículo aplicables deben ser A (automóvil), M (motocicleta) o AM (ambos)."]);
        }

        return new TiposVehiculoAplicables(normalizado);
    }

    public bool AplicaA(TipoVehiculo tipoVehiculo) => Value switch
    {
        "A" => tipoVehiculo == TipoVehiculo.Automovil,
        "M" => tipoVehiculo == TipoVehiculo.Motocicleta,
        "AM" => true,
        _ => false,
    };

    public bool Equals(TiposVehiculoAplicables? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => obj is TiposVehiculoAplicables otro && Equals(otro);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public static bool operator ==(TiposVehiculoAplicables? left, TiposVehiculoAplicables? right)
        => left is null ? right is null : left.Equals(right);

    public static bool operator !=(TiposVehiculoAplicables? left, TiposVehiculoAplicables? right)
        => !(left == right);

    public override string ToString() => Value;
}