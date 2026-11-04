using ApiPoo2.Domain.Personas;
using ApiPoo2.Domain.Rutas;
using ApiPoo2.Domain.Vehiculos;

namespace ApiPoo2.Application.UseCases.Rutas;

public sealed record ParadaOutputDto(
    Guid Id,
    Guid IdRuta,
    string Ubicacion,
    int OrdenParada,
    double? Latitud,
    double? Longitud,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc)
{
    public static ParadaOutputDto From(Parada parada) => new(
        parada.Id,
        parada.IdRuta,
        parada.Ubicacion.Value,
        parada.OrdenParada,
        parada.Latitud,
        parada.Longitud,
        parada.CreatedAtUtc,
        parada.UpdatedAtUtc);
}

public sealed record RutaConsultaOutputDto(
    Guid Id,
    string CodigoRuta,
    Guid PersonaId,
    string NumeroIdentificacionConductor,
    string Conductor,
    Guid VehiculoId,
    string Placa,
    string LoginUsuario,
    bool Operable,
    string? MotivoNoOperable,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    IReadOnlyList<ParadaOutputDto> Paradas)
{
    public static RutaConsultaOutputDto From(Ruta ruta, DateTime ahora)
        => From(ruta, ruta.Persona, ruta.Vehiculo, ahora);

    public static RutaConsultaOutputDto From(
        Ruta ruta,
        Persona? persona,
        Vehiculo? vehiculo,
        DateTime ahora)
    {
        var motivo = RutaOperabilidad.MotivoNoOperable(ruta.VehiculoId, persona, vehiculo, ahora);

        return new RutaConsultaOutputDto(
            ruta.Id,
            ruta.Codigo.Value,
            ruta.PersonaId,
            persona?.NumeroIdentificacion.Value ?? string.Empty,
            NombreConductor(persona),
            ruta.VehiculoId,
            vehiculo?.Placa.Value ?? string.Empty,
            ruta.LoginUsuario.Value,
            motivo is null,
            motivo,
            ruta.CreatedAtUtc,
            ruta.UpdatedAtUtc,
            ruta.Paradas
                .OrderBy(p => p.OrdenParada)
                .Select(ParadaOutputDto.From)
                .ToList());
    }

    private static string NombreConductor(Persona? persona)
        => persona is null
            ? string.Empty
            : $"{persona.Nombres.Value} {persona.Apellidos.Value}".Trim();
}

public sealed record RutaResumenOutputDto(
    Guid Id,
    string CodigoRuta,
    string NumeroIdentificacionConductor,
    string Conductor,
    string Placa,
    int CantidadParadas,
    bool Operable,
    string? MotivoNoOperable,
    DateTime CreatedAtUtc)
{
    public static RutaResumenOutputDto From(Ruta ruta, DateTime ahora)
        => From(ruta, ruta.Persona, ruta.Vehiculo, ahora);

    public static RutaResumenOutputDto From(
        Ruta ruta,
        Persona? persona,
        Vehiculo? vehiculo,
        DateTime ahora)
    {
        var motivo = RutaOperabilidad.MotivoNoOperable(ruta.VehiculoId, persona, vehiculo, ahora);

        return new RutaResumenOutputDto(
            ruta.Id,
            ruta.Codigo.Value,
            persona?.NumeroIdentificacion.Value ?? string.Empty,
            persona is null
                ? string.Empty
                : $"{persona.Nombres.Value} {persona.Apellidos.Value}".Trim(),
            vehiculo?.Placa.Value ?? string.Empty,
            ruta.CantidadParadas(),
            motivo is null,
            motivo,
            ruta.CreatedAtUtc);
    }
}

/// <summary>
///     Regla de operabilidad de una ruta: el conductor debe ser CONDUCTOR con licencia vigente y
///     estar habilitado (PO) para el vehículo, y el vehículo debe tener todos sus documentos
///     habilitados y vigentes. Vive en Application porque combina datos de varios agregados.
/// </summary>
internal static class RutaOperabilidad
{
    public static string? MotivoNoOperable(
        Guid vehiculoId,
        Persona? persona,
        Vehiculo? vehiculo,
        DateTime ahora)
    {
        if (persona is null || vehiculo is null)
        {
            return "La ruta no tiene conductor o vehículo asociado.";
        }

        if (!persona.EsConductor())
        {
            return "El conductor no está registrado como CONDUCTOR.";
        }

        if (!persona.LicenciaVigente(ahora))
        {
            return "El conductor no tiene una licencia de conducción vigente.";
        }

        var asociacion = persona.ConductoresVehiculosAssociated
            .FirstOrDefault(c => c.VehiculoId == vehiculoId);

        if (asociacion is null || !asociacion.PuedeOperar())
        {
            return "El conductor no está habilitado para operar el vehículo.";
        }

        var documentos = vehiculo.GetTiposDocumento();

        if (documentos.Count == 0)
        {
            return "El vehículo no tiene documentos cargados.";
        }

        if (documentos.Any(d => !d.EstaHabilitado(ahora)))
        {
            return "El vehículo tiene documentos no habilitados o vencidos.";
        }

        return null;
    }
}
