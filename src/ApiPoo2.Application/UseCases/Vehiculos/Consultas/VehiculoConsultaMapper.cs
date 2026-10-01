using ApiPoo2.Domain.Vehiculos;

namespace ApiPoo2.Application.UseCases.Vehiculos.Consultas;

internal static class VehiculoConsultaMapper
{
    public static VehiculoConsultaOutputDto FromVehiculo(Vehiculo vehiculo, DateTime ahora)
    {
        var documentos = vehiculo.GetDocumentos()
            .Select(d => new DocumentoVehiculoOutputDto(
                d.DocumentoId,
                d.NombreArchivo.Value,
                d.Contenido.ContentType,
                d.FechaExpedicion,
                d.FechaVencimiento,
                d.EstadoActual(ahora),
                d.Contenido.Length))
            .ToList();

        var conductores = vehiculo.GetConductores()
            .Select(c => new ConductorVehiculoOutputDto(
                c.PersonaId,
                c.Persona?.Nombres.Value ?? string.Empty,
                c.Persona?.Apellidos.Value ?? string.Empty,
                c.FechaAsociacion,
                c.Estado))
            .ToList();

        return new VehiculoConsultaOutputDto(
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
            documentos,
            conductores);
    }
}
