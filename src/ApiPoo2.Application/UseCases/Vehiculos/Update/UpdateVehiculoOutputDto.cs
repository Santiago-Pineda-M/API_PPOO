using ApiPoo2.Domain.Vehiculos;

namespace ApiPoo2.Application.UseCases.Vehiculos.Update;

public sealed record UpdateVehiculoOutputDto(
    Guid Id,
    string Placa,
    TipoVehiculo TipoVehiculo,
    TipoServicio TipoServicio,
    TipoCombustible TipoCombustible,
    int CapacidadPasajeros,
    string Color,
    int Modelo,
    string Marca,
    string Linea,
    DateTime? UpdatedAtUtc)
{
    public static UpdateVehiculoOutputDto From(Vehiculo vehiculo) => new(
        vehiculo.Id,
        vehiculo.Placa.Value,
        vehiculo.TipoVehiculo,
        vehiculo.TipoServicio,
        vehiculo.Combustible,
        vehiculo.CapacidadPasajeros,
        vehiculo.Color.Value,
        vehiculo.Modelo,
        vehiculo.Marca.Value,
        vehiculo.Linea.Value,
        vehiculo.UpdatedAtUtc);
}
