using ApiPoo2.Domain.Common;
using ApiPoo2.Domain.Vehiculos;

namespace ApiPoo2.Domain.Personas;

/// <summary>
///     Estado del conductor respecto al vehículo, según el enunciado.
/// </summary>
public enum EstadoConductor
{
    /// <summary>PO — Puede Operar.</summary>
    Po = 1,

    /// <summary>EA — Espera de Aprobación.</summary>
    Ea = 2,

    /// <summary>RO — Restringido para Operar.</summary>
    Ro = 3,
}
