using ApiPoo2.Domain.Common;
using ApiPoo2.Domain.Users;

namespace ApiPoo2.Domain.Personas;

public sealed class Persona : BaseEntity
{
    public TipoIdentificacion TipoIdentificacion { get; private set; }

    public NumeroIdentificacion NumeroIdentificacion { get; private set; } = null!;

    public Nombres Nombres { get; private set; } = null!;

    public Apellidos Apellidos { get; private set; } = null!;

    public CorreoElectronico CorreoElectronico { get; private set; } = null!;

    public TipoPersona TipoPersona { get; private set; }

    public User? Usuario { get; private set; }

    /// <summary>Navegación de solo lectura; EF la materializa y nadie puede mutarla desde afuera.</summary>
    public IReadOnlyList<ConductorVehiculo> ConductoresVehiculosAssociated => ConductoresVehiculos;

    public bool EsConductor() => TipoPersona == TipoPersona.Conductor;

    private List<ConductorVehiculo> ConductoresVehiculos { get; set; } = [];

    private Persona()
    {
    }

    public static Persona Register(
        TipoIdentificacion tipoIdentificacion,
        string numeroIdentificacion,
        string nombres,
        string apellidos,
        string correoElectronico,
        TipoPersona tipoPersona,
        DateTime utcNow)
    {
        var persona = new Persona
        {
            TipoIdentificacion = tipoIdentificacion,
            NumeroIdentificacion = NumeroIdentificacion.From(numeroIdentificacion),
            Nombres = Nombres.From(nombres),
            Apellidos = Apellidos.From(apellidos),
            CorreoElectronico = CorreoElectronico.From(correoElectronico),
            TipoPersona = tipoPersona,
        };

        persona.Initialize(Guid.NewGuid(), utcNow);
        return persona;
    }

    public bool PuedeTenerUsuario() => TipoPersona == TipoPersona.Administrativo;

    /// <summary>
    ///     Actualiza los datos editables de la persona. El número de identificación no se cambia aquí
    ///     porque alimenta el login mnemotécnico del usuario asociado.
    /// </summary>
    public void ActualizarDatos(string nombres, string apellidos, string correoElectronico, DateTime utcNow)
    {
        Nombres = Nombres.From(nombres);
        Apellidos = Apellidos.From(apellidos);
        CorreoElectronico = CorreoElectronico.From(correoElectronico);
        MarkUpdated(utcNow);
    }

    public User CrearUsuario(string passwordHash, DateTime utcNow, int sufijoLogin = 0)
    {
        if (!PuedeTenerUsuario())
        {
            throw new DomainValidationException(
                "persona.usuario.no_admitido",
                [$"Una persona de tipo {TipoPersona} no puede tener un usuario asociado."]);
        }

        if (Usuario is not null)
        {
            throw new DomainValidationException(
                "persona.usuario.duplicado",
                ["La persona ya tiene un usuario asociado."]);
        }

        var login = Login.FromMnemonic(Nombres.Value, Apellidos.Value, NumeroIdentificacion, sufijoLogin);

        Usuario = User.Crear(Id, login, passwordHash, UserRole.Administrator, utcNow);
        MarkUpdated(utcNow);

        return Usuario;
    }

    public void CambiarTipoPersona(TipoPersona tipo, DateTime utcNow)
    {
        if (TipoPersona == tipo)
        {
            return;
        }

        if (Usuario is not null && tipo != TipoPersona.Administrativo)
        {
            throw new DomainValidationException(
                "persona.usuario.requerido",
                ["No se puede quitar el tipo ADMINISTRATIVO a una persona que ya tiene un usuario asociado."]);
        }

        TipoPersona = tipo;
        MarkUpdated(utcNow);
    }
}
