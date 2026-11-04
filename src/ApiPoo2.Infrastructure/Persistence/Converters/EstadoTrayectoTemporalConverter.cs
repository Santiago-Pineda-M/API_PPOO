using ApiPoo2.Domain.Trayectos;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ApiPoo2.Infrastructure.Persistence.Converters;

public sealed class EstadoTrayectoTemporalConverter : ValueConverter<EstadoTrayectoTemporal, string>
{
    private static readonly Dictionary<EstadoTrayectoTemporal, string> A = new()
    {
        [EstadoTrayectoTemporal.Cargado] = "CARGADO",
        [EstadoTrayectoTemporal.Validado] = "VALIDADO",
        [EstadoTrayectoTemporal.Procesado] = "PROCESADO",
        [EstadoTrayectoTemporal.Error] = "ERROR",
    };

    private static readonly Dictionary<string, EstadoTrayectoTemporal> B =
        A.ToDictionary(x => x.Value, x => x.Key);

    public EstadoTrayectoTemporalConverter()
        : base(v => A[v], v => B[v])
    {
    }
}
