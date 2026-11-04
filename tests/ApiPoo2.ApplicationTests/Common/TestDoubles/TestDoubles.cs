using System.Linq.Expressions;
using ApiPoo2.Application.Common;
using ApiPoo2.Application.IRepositories;
using ApiPoo2.Application.IServices;
using ApiPoo2.Domain.BlacklistedTokens;
using ApiPoo2.Domain.Personas;
using ApiPoo2.Domain.RefreshTokens;
using ApiPoo2.Domain.Rutas;
using ApiPoo2.Domain.TiposDocumento;
using ApiPoo2.Domain.Trayectos;
using ApiPoo2.Domain.Users;
using ApiPoo2.Domain.Vehiculos;
using Microsoft.Extensions.Logging;

namespace ApiPoo2.ApplicationTests.Common.TestDoubles;

public static class TestData
{
    public static readonly DateTime Ahora = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    public static readonly byte[] PdfValido = "%PDF-1.7 contenido"u8.ToArray();

    public static ILoggerFactory LoggerFactory { get; } = Microsoft.Extensions.Logging.LoggerFactory.Create(_ => { });
}

public sealed class FakeDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow { get; set; } = TestData.Ahora;
}

public sealed class FakePasswordHasher : IPasswordHasher
{
    public int HashCalls { get; private set; }

    public string Hash(string password)
    {
        HashCalls++;
        return "HASH:" + password;
    }

    public bool Verify(string password, string passwordHash) => passwordHash == "HASH:" + password;
}

public sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveChangesCalls { get; private set; }

    public Exception? ThrowOnSave { get; set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesCalls++;

        if (ThrowOnSave is not null)
        {
            return Task.FromException<int>(ThrowOnSave);
        }

        return Task.FromResult(0);
    }
}

public sealed class FakePersonaRepository : IPersonaRepository
{
    public List<Persona> Items { get; } = [];

    public Task<Persona?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult<Persona?>(Items.FirstOrDefault(p => p.Id == id));

    public Task<Persona?> GetByNumeroIdentificacionAsync(
        NumeroIdentificacion numero,
        CancellationToken cancellationToken = default)
        => Task.FromResult<Persona?>(Items.FirstOrDefault(p => p.NumeroIdentificacion.Value == numero.Value));

    public Task<Persona?> GetByCorreoAsync(
        CorreoElectronico correo,
        CancellationToken cancellationToken = default)
        => Task.FromResult<Persona?>(Items.FirstOrDefault(p => p.CorreoElectronico.Value == correo.Value));

    public Task<bool> ExistsByNumeroIdentificacionAsync(
        NumeroIdentificacion numero,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Items.Any(p => p.NumeroIdentificacion.Value == numero.Value));

    public Task<bool> ExistsByLoginAsync(Login login, CancellationToken cancellationToken = default)
        => Task.FromResult(Items.Any(p => p.Usuario != null && p.Usuario.Login.Value == login.Value));

    public Task<IReadOnlyDictionary<TipoPersona, int>> CountByTipoAsync(
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyDictionary<TipoPersona, int>>(
            Items.GroupBy(p => p.TipoPersona).ToDictionary(g => g.Key, g => g.Count()));

    public Task<(IReadOnlyList<Persona> Items, int Total)> GetPersonasConUsuarioPaginadoAsync(
        PagedFilter filtro,
        CancellationToken cancellationToken = default)
    {
        var query = Items.Where(p => p.Usuario is not null);
        var total = query.Count();
        var items = query.Skip(filtro.Skip).Take(filtro.PageSize).ToList();
        return Task.FromResult((items as IReadOnlyList<Persona>, total));
    }

    public Task<int> CountPersonasConUsuarioAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Items.Count(p => p.Usuario is not null));

    public Task<IReadOnlyList<Persona>> GetConLicenciaVencidaAsync(
        DateTime utcNow,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Persona>>(
            Items.Where(p => p.Licencia is not null
                && p.FechaVigenciaLicencia.HasValue
                && p.FechaVigenciaLicencia.Value <= utcNow).ToList());

    public Task<bool> AnyAsync(
        Expression<Func<Persona, bool>> predicate,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Items.AsQueryable().Any(predicate));

    public void Add(Persona entity) => Items.Add(entity);

    public void Update(Persona entity)
    {
    }

    public void Remove(Persona entity) => Items.Remove(entity);
}

public sealed class FakeUserRepository : IUserRepository
{
    public List<User> Items { get; } = [];

    public Task<User?> GetByPersonaAndLoginAsync(
        Guid idPersona,
        Login login,
        CancellationToken cancellationToken = default)
        => Task.FromResult<User?>(Items.FirstOrDefault(u =>
            u.IdPersona == idPersona && u.Login.Value == login.Value));

    public Task<User?> GetByLoginAsync(Login login, CancellationToken cancellationToken = default)
        => Task.FromResult<User?>(Items.FirstOrDefault(u => u.Login.Value == login.Value));

    public Task<Persona?> GetPersonaByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult<Persona?>(null);

    public Task<User?> GetByApiKeyAsync(ApiKey apiKey, CancellationToken cancellationToken = default)
        => Task.FromResult<User?>(Items.FirstOrDefault(u => u.ApiKey.Value == apiKey.Value));

    public Task<IReadOnlyList<User>> GetByPersonaAsync(
        Guid idPersona,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<User>>(Items.Where(u => u.IdPersona == idPersona).ToList());

    public void Add(User user) => Items.Add(user);
}

public sealed class FakeVehiculoRepository : IVehiculoRepository
{
    public List<Vehiculo> Items { get; } = [];

    public Task<Vehiculo?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult<Vehiculo?>(Items.FirstOrDefault(v => v.Id == id));

    public Task<Vehiculo?> GetByPlacaAsync(Placa placa, CancellationToken cancellationToken = default)
        => Task.FromResult<Vehiculo?>(Items.FirstOrDefault(v => v.Placa.Value == placa.Value));

    public Task<IReadOnlyList<Vehiculo>> GetByTipoAsync(
        TipoVehiculo tipo,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Vehiculo>>(Items.Where(v => v.TipoVehiculo == tipo).ToList());

    public Task<IReadOnlyList<Vehiculo>> GetAllWithDetailsAsync(
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Vehiculo>>([.. Items]);

    public Task<IReadOnlyList<Vehiculo>> GetAllWithDetailsForUpdateAsync(
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Vehiculo>>([.. Items]);

    public void Add(Vehiculo vehiculo) => Items.Add(vehiculo);

    public void Remove(Vehiculo vehiculo) => Items.Remove(vehiculo);
}

public sealed class FakeTipoDocumentoRepository : ITipoDocumentoRepository
{
    public List<TipoDocumento> Items { get; } = [];

    public List<DocumentoVehiculo> DocumentosVehiculo { get; } = [];

    public List<Vehiculo> Vehiculos { get; } = [];

    public DateTime? UltimoUtcNowConsultado { get; private set; }

    public int? UltimosDiasConsultados { get; private set; }

    public Task<TipoDocumento?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult<TipoDocumento?>(Items.FirstOrDefault(d => d.Id == id));

    public Task<TipoDocumento?> GetByCodigoAsync(TipoDocumentoCodigo codigo, CancellationToken cancellationToken = default)
        => Task.FromResult<TipoDocumento?>(Items.FirstOrDefault(d => d.Codigo.Value == codigo.Value));

    public Task<IReadOnlyList<TipoDocumento>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<TipoDocumento>>([.. Items]);

    public Task<IReadOnlyList<DocumentoVehiculo>> GetByVehiculoAsync(
        Guid vehiculoId,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<DocumentoVehiculo>>(
            DocumentosVehiculo.Where(d => d.VehiculoId == vehiculoId).ToList());

    public Task<IReadOnlyList<DocumentoVehiculo>> GetVencidosAsync(
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        UltimoUtcNowConsultado = utcNow;
        return Task.FromResult<IReadOnlyList<DocumentoVehiculo>>(
            DocumentosVehiculo.Where(d => d.EstadoActual(utcNow) == EstadoDocumento.Vencido).ToList());
    }

    public Task<IReadOnlyList<DocumentoVehiculo>> GetPorVencerAsync(
        DateTime utcNow,
        int dias,
        CancellationToken cancellationToken = default)
    {
        UltimoUtcNowConsultado = utcNow;
        UltimosDiasConsultados = dias;
        return Task.FromResult<IReadOnlyList<DocumentoVehiculo>>([.. DocumentosVehiculo]);
    }

    public Task<IReadOnlyList<Vehiculo>> GetConTipoDocumentoAsync(
        TipoDocumentoCodigo codigo,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Vehiculo>>(Vehiculos);

    public Task<bool> ExistsByTipoDocumentoIdAsync(Guid tipoDocumentoId, CancellationToken cancellationToken = default)
        => Task.FromResult(DocumentosVehiculo.Any(d => d.TipoDocumentoId == tipoDocumentoId));

    public void Add(TipoDocumento documento) => Items.Add(documento);

    public void Remove(TipoDocumento documento) => Items.Remove(documento);
}

public sealed class FakeConductorVehiculoRepository : IConductorVehiculoRepository
{
    public List<ConductorVehiculo> Items { get; } = [];

    public List<Persona> Personas { get; } = [];

    public Task<IReadOnlyList<ConductorVehiculo>> GetByPersonaAsync(
        Guid personaId,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ConductorVehiculo>>(
            Items.Where(c => c.PersonaId == personaId).ToList());

    public Task<IReadOnlyList<ConductorVehiculo>> GetOperablesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ConductorVehiculo>>(Items.Where(c => c.PuedeOperar()).ToList());

    public Task<ConductorVehiculo?> GetByPersonaAndVehiculoAsync(
        Guid personaId,
        Guid vehiculoId,
        CancellationToken cancellationToken = default)
        => Task.FromResult<ConductorVehiculo?>(Items.FirstOrDefault(c =>
            c.PersonaId == personaId && c.VehiculoId == vehiculoId));

    public void Add(ConductorVehiculo conductorVehiculo) => Items.Add(conductorVehiculo);
}

public sealed record ConductorVehiculoConPersona(ConductorVehiculo Relacion, Persona? Persona);

public static class ConductorVehiculoTestExtensions
{
    private static readonly System.Reflection.PropertyInfo PersonaProperty =
        typeof(ConductorVehiculo).GetProperty(nameof(ConductorVehiculo.Persona))!;

    public static ConductorVehiculo ConPersona(this ConductorVehiculo relacion, Persona persona)
    {
        PersonaProperty.SetValue(relacion, persona);
        return relacion;
    }
}

public sealed class FakeBlacklistedTokenRepository : IBlacklistedTokenRepository
{
    public List<BlacklistedToken> Items { get; } = [];

    public Task<BlacklistedToken?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult<BlacklistedToken?>(Items.FirstOrDefault(b => b.Id == id));

    public Task<bool> AnyAsync(
        Expression<Func<BlacklistedToken, bool>> predicate,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Items.AsQueryable().Any(predicate));

    public Task<bool> IsBlacklistedAsync(Guid jti, CancellationToken cancellationToken = default)
        => Task.FromResult(Items.Any(b => b.Jti == jti));

    public Task<int> PurgeExpiredAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var purged = Items.RemoveAll(b => b.IsExpired(utcNow));
        return Task.FromResult(purged);
    }

    public void Add(BlacklistedToken entity) => Items.Add(entity);

    public void Update(BlacklistedToken entity)
    {
    }

    public void Remove(BlacklistedToken entity) => Items.Remove(entity);
}

public sealed class FakeRefreshTokenRepository : IRefreshTokenRepository
{
    public List<RefreshToken> Items { get; } = [];

    public Task<RefreshToken?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult<RefreshToken?>(Items.FirstOrDefault(t => t.Id == id));

    public Task<bool> AnyAsync(
        Expression<Func<RefreshToken, bool>> predicate,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Items.AsQueryable().Any(predicate));

    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default)
        => Task.FromResult<RefreshToken?>(Items.FirstOrDefault(t => t.TokenHash == tokenHash));

    public Task<IReadOnlyList<RefreshToken>> GetByPersonaAsync(
        Guid idPersona,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<RefreshToken>>(Items.Where(t => t.PersonaId == idPersona).ToList());

    public Task<int> DeleteExpiredAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var deleted = Items.RemoveAll(t => t.ExpiresAtUtc <= utcNow);
        return Task.FromResult(deleted);
    }

    public void Add(RefreshToken entity) => Items.Add(entity);

    public void Update(RefreshToken entity)
    {
    }

    public void Remove(RefreshToken entity) => Items.Remove(entity);
}

public sealed class FakeRutaRepository : IRutaRepository
{
    public List<Ruta> Items { get; } = [];

    public Task<Ruta?> GetByCodigoAsync(CodigoRuta codigo, CancellationToken cancellationToken = default)
        => Task.FromResult<Ruta?>(Items.FirstOrDefault(r => r.Codigo == codigo));

    public Task<Ruta?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult<Ruta?>(Items.FirstOrDefault(r => r.Id == id));

    public Task<Ruta?> GetByParadaAsync(Guid paradaId, CancellationToken cancellationToken = default)
        => Task.FromResult<Ruta?>(Items.FirstOrDefault(r => r.Paradas.Any(p => p.Id == paradaId)));

    public Task<bool> ExistsAsync(CodigoRuta codigo, CancellationToken cancellationToken = default)
        => Task.FromResult(Items.Any(r => r.Codigo == codigo));

    public Task<IReadOnlyList<Ruta>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Ruta>>([.. Items]);

    public Task<IReadOnlyList<Ruta>> GetByPersonaAsync(
        Guid personaId,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Ruta>>(Items.Where(r => r.PersonaId == personaId).ToList());

    public Task<IReadOnlyList<Ruta>> GetByPlacaAsync(
        Placa placa,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Ruta>>(
            Items.Where(r => r.Vehiculo?.Placa.Value == placa.Value).ToList());

    public Task<IReadOnlyList<ParadaPendienteGeocodificar>> GetPendientesCoordenadasAsync(
        int limite,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ParadaPendienteGeocodificar>>(
            Items
                .SelectMany(r => r.Paradas
                    .Where(p => !p.TieneCoordenadas())
                    .Select(p => new ParadaPendienteGeocodificar(
                        r.Id,
                        p.Id,
                        p.Ubicacion.Value,
                        p.CreatedAtUtc)))
                .OrderBy(p => p.CreadaUtc)
                .Take(limite)
                .ToList());

    public void Add(Ruta ruta) => Items.Add(ruta);

    public List<Parada> ParadasAgregadas { get; } = [];

    public void AddParada(Parada parada) => ParadasAgregadas.Add(parada);

    public void Remove(Ruta ruta) => Items.Remove(ruta);
}

public sealed class FakeTrayectoTemporalRepository : ITrayectoTemporalRepository
{
    public List<TrayectoTemporal> Items { get; } = [];

    public int Eliminados { get; private set; }

    public Task<bool> ExisteCargueAsync(long idCargue, CancellationToken cancellationToken = default)
        => Task.FromResult(Items.Any(t => t.IdCargue == idCargue));

    public Task<IReadOnlyList<TrayectoTemporal>> GetByIdCargueAsync(
        long idCargue,
        EstadoTrayectoTemporal? estado = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<TrayectoTemporal>>(
            Items
                .Where(t => t.IdCargue == idCargue)
                .Where(t => estado is null || t.Estado == estado.Value)
                .OrderBy(t => t.CreatedAtUtc)
                .ToList());

    public Task<IReadOnlyList<CargueResumen>> GetResumenesAsync(
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<CargueResumen>>(
            Items
                .GroupBy(t => t.IdCargue)
                .Select(g => new CargueResumen(
                    g.Key,
                    g.Min(t => t.CreatedAtUtc),
                    g.Count(),
                    g.Count(t => t.Estado == EstadoTrayectoTemporal.Cargado),
                    g.Count(t => t.Estado == EstadoTrayectoTemporal.Validado),
                    g.Count(t => t.Estado == EstadoTrayectoTemporal.Procesado),
                    g.Count(t => t.Estado == EstadoTrayectoTemporal.Error)))
                .OrderByDescending(r => r.IdCargue)
                .ToList());

    public Task<int> EliminarCargueAsync(long idCargue, CancellationToken cancellationToken = default)
    {
        var eliminados = Items.RemoveAll(t => t.IdCargue == idCargue);
        Eliminados += eliminados;
        return Task.FromResult(eliminados);
    }
}

public sealed class FakeExcelTrayectoParser : IExcelTrayectoParser
{
    public List<FilaTrayectoExcel> Filas { get; } = [];

    public Exception? ThrowOnParse { get; set; }

    public string? UltimoNombreArchivo { get; private set; }

    public IReadOnlyList<FilaTrayectoExcel> Parse(Stream contenido, string nombreArchivo)
    {
        UltimoNombreArchivo = nombreArchivo;

        if (ThrowOnParse is not null)
        {
            throw ThrowOnParse;
        }

        return [.. Filas];
    }
}

public sealed class FakeCargueTrayectosProcedures : ICargueTrayectosProcedures
{
    public ResumenCargue Resumen { get; set; } = new(0, 0, 0, 0);

    public long? UltimoIdCargue { get; private set; }

    public IReadOnlyList<FilaTrayectoExcel> UltimasFilas { get; private set; } = [];

    public Task<ResumenCargue> EjecutarAsync(
        long idCargue,
        IReadOnlyList<FilaTrayectoExcel> filas,
        CancellationToken cancellationToken = default)
    {
        UltimoIdCargue = idCargue;
        UltimasFilas = filas;

        return Task.FromResult(Resumen);
    }
}

public sealed class FakeGeocodingService : IGeocodingService
{
    public Dictionary<string, Coordenadas?> Coordenadas { get; } = new(StringComparer.OrdinalIgnoreCase);

    public List<string> Consultas { get; } = [];

    public Exception? ThrowOnGeocode { get; set; }

    public Task<Coordenadas?> GeocodeAsync(
        string ubicacion,
        CancellationToken cancellationToken = default)
    {
        Consultas.Add(ubicacion);

        if (ThrowOnGeocode is not null)
        {
            return Task.FromException<Coordenadas?>(ThrowOnGeocode);
        }

        return Task.FromResult(Coordenadas.GetValueOrDefault(ubicacion));
    }
}

public sealed class FakeEmailNotifier : IEmailNotifier
{
    public List<(string Correo, string Login)> Notificaciones { get; } = [];

    public Task NotificarRestriccionAsync(
        string correo,
        string login,
        CancellationToken cancellationToken = default)
    {
        Notificaciones.Add((correo, login));
        return Task.CompletedTask;
    }
}

/// <summary>Fábrica de agregados válidos para los casos de uso de rutas y cargues.</summary>
public static class DominioDePrueba
{
    public static Persona Conductor(
        string identificacion = "1234567890",
        string correo = "conductor@example.com")
        => Persona.Register(
            TipoIdentificacion.CedulaCiudadania,
            identificacion,
            "Carlos",
            "Ríos",
            correo,
            TipoPersona.Conductor,
            TestData.Ahora);

    public static Persona ConductorConLicencia(
        string identificacion = "1234567890",
        string correo = "conductor@example.com",
        DateTime? vigencia = null)
    {
        var persona = Conductor(identificacion, correo);
        persona.RegistrarLicencia(
            TestData.PdfValido,
            vigencia ?? TestData.Ahora.AddMonths(6),
            TestData.Ahora);

        return persona;
    }

    public static Vehiculo Vehiculo(string placa = "ABC123")
        => Domain.Vehiculos.Vehiculo.Register(
            placa,
            TipoVehiculo.Automovil,
            TipoServicio.Privado,
            TipoCombustible.Gasolina,
            5,
            "#FF5733",
            2020,
            "Toyota",
            "Fortuner SW",
            TestData.Ahora);

    public static Vehiculo VehiculoConDocumentos(
        TipoDocumento documento,
        string placa = "ABC123",
        DateTime? vencimiento = null,
        EstadoDocumento estado = EstadoDocumento.Habilitado)
    {
        var vehiculo = Vehiculo(placa);
        var relacion = vehiculo.AdjuntarDocumento(
            documento,
            TestData.PdfValido,
            "SOAT.pdf",
            TestData.Ahora.AddDays(-10),
            vencimiento ?? TestData.Ahora.AddMonths(6),
            TestData.Ahora);

        if (estado != EstadoDocumento.EnVerificacion)
        {
            relacion.CambiarEstado(estado, TestData.Ahora);
        }

        return vehiculo;
    }

    public static TipoDocumento TipoDocumento(string codigo = "SOAT")
        => Domain.TiposDocumento.TipoDocumento.Register(
            codigo,
            codigo,
            "AM",
            "RR",
            "Seguro Obligatorio",
            TestData.Ahora);

    public static ConductorVehiculo AsociarOperable(Persona persona, Vehiculo vehiculo)
    {
        var relacion = ConductorVehiculo.Crear(
            persona.Id,
            vehiculo.Id,
            TestData.Ahora,
            EstadoConductor.Po);

        persona.AgregarAsociacion(relacion);
        return relacion;
    }

    public static Ruta RutaOperable(
        string codigo = "R-001",
        string ubicacionInicial = "Terminal de Ibagué",
        string identificacion = "1234567890",
        string placa = "ABC123")
    {
        var documento = TipoDocumento();
        var persona = ConductorConLicencia(identificacion);
        var vehiculo = VehiculoConDocumentos(documento, placa);
        AsociarOperable(persona, vehiculo);

        var ruta = Ruta.Registrar(
            codigo,
            persona.Id,
            vehiculo.Id,
            "admin",
            ubicacionInicial,
            null,
            null,
            "Destino",
            null,
            null,
            TestData.Ahora);

        return ruta.ConNavegaciones(persona, vehiculo);
    }

    /// <summary>Otra ruta del mismo conductor y vehículo, para agrupar varias por consulta.</summary>
    public static Ruta RutaDelMismoConductor(string codigo, Ruta original)
        => Ruta.Registrar(
            codigo,
            original.PersonaId,
            original.VehiculoId,
            "admin",
            original.Paradas.Single(p => p.EsInicial()).Ubicacion.Value,
            null,
            null,
            "Destino",
            null,
            null,
            TestData.Ahora).ConNavegaciones(original.Persona, original.Vehiculo);
}

public static class RutaTestExtensions
{
    private static readonly System.Reflection.PropertyInfo PersonaProperty =
        typeof(Ruta).GetProperty(nameof(Ruta.Persona))!;

    private static readonly System.Reflection.PropertyInfo VehiculoProperty =
        typeof(Ruta).GetProperty(nameof(Ruta.Vehiculo))!;

    public static Ruta ConNavegaciones(this Ruta ruta, Persona persona, Vehiculo vehiculo)
    {
        PersonaProperty.SetValue(ruta, persona);
        VehiculoProperty.SetValue(ruta, vehiculo);
        return ruta;
    }
}

public static class PersonaTestExtensions
{
    private static readonly System.Reflection.PropertyInfo AsociacionesProperty =
        typeof(Persona).GetProperty(
            "ConductoresVehiculos",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;

    public static void AgregarAsociacion(this Persona persona, ConductorVehiculo relacion)
    {
        var lista = (List<ConductorVehiculo>)AsociacionesProperty.GetValue(persona)!;
        lista.Add(relacion);
    }
}
