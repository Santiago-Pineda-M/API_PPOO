using ApiPoo2.Domain.Personas;
using ApiPoo2.Domain.Vehiculos;

namespace ApiPoo2.Application.UseCases.Rutas;

/// <summary>
///     Reglas compartidas por el alta y la reasignación de rutas. Se reutilizan códigos de error
///     estables para que la WebApi y las pruebas de integración los puedan fijar.
/// </summary>
internal static class RutaOperacionValidator
{
    public static void Validar(
        Persona persona,
        Vehiculo vehiculo,
        ConductorVehiculo? asociacion,
        DateTime ahora)
    {
        if (!persona.EsConductor())
        {
            throw new ForbiddenException(
                "ruta.conductor.invalido",
                "El conductor debe ser una persona de tipo CONDUCTOR.");
        }

        if (!persona.LicenciaVigente(ahora))
        {
            throw new ConflictException(
                "ruta.licencia.vencida",
                "El conductor no tiene una licencia de conducción vigente.");
        }

        if (asociacion is null || !asociacion.PuedeOperar())
        {
            throw new ConflictException(
                "ruta.conductor.no_operable",
                "El conductor no está habilitado para operar el vehículo.");
        }

        var documentos = vehiculo.GetTiposDocumento();

        if (documentos.Count == 0 || documentos.Any(d => !d.EstaHabilitado(ahora)))
        {
            throw new ConflictException(
                "ruta.documentos.no_habilitados",
                "El vehículo no tiene todos sus documentos habilitados y vigentes.");
        }
    }
}
