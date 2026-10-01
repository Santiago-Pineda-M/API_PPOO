using ApiPoo2.Domain.Common;

namespace ApiPoo2.Domain.Personas;

/// <summary>
///     Relación N:M entre un conductor y un vehículo, con los atributos complementarios que pide el
///     enunciado: fecha de asociación y estado del conductor.
/// </summary>
public sealed class ConductorVehiculo : BaseEntity
{
    public Guid PersonaId { get; private set; }

    public Guid VehiculoId { get; private set; }

    public DateTime FechaAsociacion { get; private set; }

    public EstadoConductor Estado { get; private set; }

    public Persona Persona { get; private set; } = null!;

    private ConductorVehiculo()
    {
    }

    public static ConductorVehiculo Crear(
        Guid personaId,
        Guid vehiculoId,
        DateTime utcNow,
        EstadoConductor estado = EstadoConductor.Ea)
    {
        if (personaId == Guid.Empty)
        {
            throw new DomainValidationException(
                "conductor.persona",
                ["La persona del conductor es obligatoria."]);
        }

        if (vehiculoId == Guid.Empty)
        {
            throw new DomainValidationException(
                "conductor.vehiculo",
                ["El vehículo es obligatorio."]);
        }

        var conductor = new ConductorVehiculo
        {
            PersonaId = personaId,
            VehiculoId = vehiculoId,
            FechaAsociacion = utcNow,
            Estado = estado,
        };

        conductor.Initialize(Guid.NewGuid(), utcNow);
        return conductor;
    }

    public void CambiarEstado(EstadoConductor estado, DateTime utcNow)
    {
        if (Estado == estado)
        {
            return;
        }

        Estado = estado;
        MarkUpdated(utcNow);
    }

    public bool PuedeOperar() => Estado == EstadoConductor.Po;
}
