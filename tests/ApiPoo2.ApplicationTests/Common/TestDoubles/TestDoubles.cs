using System.Linq.Expressions;
using ApiPoo2.Application.Common;
using ApiPoo2.Application.IRepositories;
using ApiPoo2.Application.IServices;
using ApiPoo2.Domain.BlacklistedTokens;
using ApiPoo2.Domain.Personas;
using ApiPoo2.Domain.RefreshTokens;
using ApiPoo2.Domain.TiposDocumento;
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
