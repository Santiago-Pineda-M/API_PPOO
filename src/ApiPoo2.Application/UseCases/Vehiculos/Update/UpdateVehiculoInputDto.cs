using ApiPoo2.Domain.Vehiculos;

namespace ApiPoo2.Application.UseCases.Vehiculos.Update;

public sealed record UpdateVehiculoInputDto(
    Guid VehiculoId,
    string Placa,
    TipoVehiculo TipoVehiculo,
    TipoServicio TipoServicio,
    TipoCombustible TipoCombustible,
    int CapacidadPasajeros,
    string Color,
    int Modelo,
    string Marca,
    string Linea);
