using ApiPoo2.Domain.Common;

namespace ApiPoo2.Domain.Vehiculos;

/// <summary>
///     Placa del vehículo. El formato depende del tipo: tres letras y tres dígitos para automóvil
///     (ABC123), o tres letras, dos dígitos y una letra para motocicleta (ABC12D).
/// </summary>
public sealed class Placa
{
    public const int Length = 6;

    public string Value { get; }

    private Placa(string value) => Value = value;

    public static Placa From(string value, TipoVehiculo tipoVehiculo)
    {
        var errors = new List<string>();
        var normalizada = value?.Trim().ToUpperInvariant() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizada))
        {
            errors.Add("La placa es obligatoria.");
            throw new DomainValidationException("vehiculo.placa", errors);
        }

        if (normalizada.Length != Length)
        {
            errors.Add($"La placa debe tener exactamente {Length} caracteres.");
        }
        else if (tipoVehiculo == TipoVehiculo.Automovil && !EsFormatoAutomovil(normalizada))
        {
            errors.Add("Para automóvil la placa debe tener tres letras seguidas de tres números. Ejemplo: ABC123.");
        }
        else if (tipoVehiculo == TipoVehiculo.Motocicleta && !EsFormatoMotocicleta(normalizada))
        {
            errors.Add("Para motocicleta la placa debe tener tres letras, dos números y una letra. Ejemplo: ABC12D.");
        }

        if (errors.Count > 0)
        {
            throw new DomainValidationException("vehiculo.placa", errors);
        }

        return new Placa(normalizada);
    }

    /// <summary>
    ///     Normaliza una placa para búsqueda cuando aún no se conoce el tipo del vehículo.
    ///     Acepta cualquiera de los dos formatos válidos.
    /// </summary>
    public static Placa FromCualquiera(string value)
    {
        var normalizada = value?.Trim().ToUpperInvariant() ?? string.Empty;

        if (normalizada.Length == Length
            && (EsFormatoAutomovil(normalizada) || EsFormatoMotocicleta(normalizada)))
        {
            return new Placa(normalizada);
        }

        throw new DomainValidationException(
            "vehiculo.placa",
            ["La placa debe tener seis caracteres con formato ABC123 o ABC12D."]);
    }

    public bool EsValidaPara(TipoVehiculo tipoVehiculo)
        => tipoVehiculo == TipoVehiculo.Automovil
            ? EsFormatoAutomovil(Value)
            : EsFormatoMotocicleta(Value);

    private static bool EsFormatoAutomovil(string value)
        => value[..3].All(char.IsLetter) && value[3..].All(char.IsDigit);

    private static bool EsFormatoMotocicleta(string value)
        => value[..3].All(char.IsLetter)
           && value[3..5].All(char.IsDigit)
           && char.IsLetter(value[5]);

    public bool Equals(Placa? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => obj is Placa otro && Equals(otro);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public static bool operator ==(Placa? left, Placa? right)
        => left is null ? right is null : left.Equals(right);

    public static bool operator !=(Placa? left, Placa? right) => !(left == right);

    public override string ToString() => Value;
}