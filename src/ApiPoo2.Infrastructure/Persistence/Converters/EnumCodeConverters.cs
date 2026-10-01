using ApiPoo2.Domain.TiposDocumento;
using ApiPoo2.Domain.Personas;
using ApiPoo2.Domain.Vehiculos;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ApiPoo2.Infrastructure.Persistence.Converters;

/// <summary>
///     Los CHECK constraints de PostgreSQL exigen códigos cortos (CC, PO, AUTOMOVIL), no los nombres
///     completos del enum. Estos converters traducen en ambos sentidos.
/// </summary>
public sealed class TipoIdentificacionConverter : ValueConverter<TipoIdentificacion, string>
{
    private static readonly Dictionary<TipoIdentificacion, string> A = new()
    {
        [TipoIdentificacion.CedulaCiudadania] = "CC",
        [TipoIdentificacion.CedulaExtranjeria] = "CE",
        [TipoIdentificacion.TarjetaIdentidad] = "TI",
        [TipoIdentificacion.Nit] = "NIT",
    };

    private static readonly Dictionary<string, TipoIdentificacion> B =
        A.ToDictionary(x => x.Value, x => x.Key);

    public TipoIdentificacionConverter()
        : base(v => A[v], v => B[v])
    {
    }
}

public sealed class TipoPersonaConverter : ValueConverter<TipoPersona, string>
{
    private static readonly Dictionary<TipoPersona, string> A = new()
    {
        [TipoPersona.Administrativo] = "ADMINISTRATIVO",
        [TipoPersona.Conductor] = "CONDUCTOR",
    };

    private static readonly Dictionary<string, TipoPersona> B =
        A.ToDictionary(x => x.Value, x => x.Key);

    public TipoPersonaConverter()
        : base(v => A[v], v => B[v])
    {
    }
}

public sealed class EstadoConductorConverter : ValueConverter<EstadoConductor, string>
{
    private static readonly Dictionary<EstadoConductor, string> A = new()
    {
        [EstadoConductor.Po] = "PO",
        [EstadoConductor.Ea] = "EA",
        [EstadoConductor.Ro] = "RO",
    };

    private static readonly Dictionary<string, EstadoConductor> B =
        A.ToDictionary(x => x.Value, x => x.Key);

    public EstadoConductorConverter()
        : base(v => A[v], v => B[v])
    {
    }
}

public sealed class EstadoDocumentoConverter : ValueConverter<EstadoDocumento, string>
{
    private static readonly Dictionary<EstadoDocumento, string> A = new()
    {
        [EstadoDocumento.Habilitado] = "HABILITADO",
        [EstadoDocumento.Vencido] = "VENCIDO",
        [EstadoDocumento.EnVerificacion] = "EN_VERIFICACION",
    };

    private static readonly Dictionary<string, EstadoDocumento> B =
        A.ToDictionary(x => x.Value, x => x.Key);

    public EstadoDocumentoConverter()
        : base(v => A[v], v => B[v])
    {
    }
}

public sealed class TipoVehiculoConverter : ValueConverter<TipoVehiculo, string>
{
    private static readonly Dictionary<TipoVehiculo, string> A = new()
    {
        [TipoVehiculo.Automovil] = "AUTOMOVIL",
        [TipoVehiculo.Motocicleta] = "MOTOCIClETA",
    };

    private static readonly Dictionary<string, TipoVehiculo> B =
        A.ToDictionary(x => x.Value, x => x.Key);

    public TipoVehiculoConverter()
        : base(v => A[v], v => B[v])
    {
    }
}

public sealed class TipoServicioConverter : ValueConverter<TipoServicio, string>
{
    private static readonly Dictionary<TipoServicio, string> A = new()
    {
        [TipoServicio.Publico] = "PUBLICO",
        [TipoServicio.Privado] = "PRIVADO",
    };

    private static readonly Dictionary<string, TipoServicio> B =
        A.ToDictionary(x => x.Value, x => x.Key);

    public TipoServicioConverter()
        : base(v => A[v], v => B[v])
    {
    }
}

public sealed class TipoCombustibleConverter : ValueConverter<TipoCombustible, string>
{
    private static readonly Dictionary<TipoCombustible, string> A = new()
    {
        [TipoCombustible.Gasolina] = "GASOLINA",
        [TipoCombustible.Gas] = "GAS",
        [TipoCombustible.Disel] = "DISEL",
    };

    private static readonly Dictionary<string, TipoCombustible> B =
        A.ToDictionary(x => x.Value, x => x.Key);

    public TipoCombustibleConverter()
        : base(v => A[v], v => B[v])
    {
    }
}
