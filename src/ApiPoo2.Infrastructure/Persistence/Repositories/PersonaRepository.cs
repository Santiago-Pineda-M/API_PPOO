using ApiPoo2.Application.IRepositories;
using ApiPoo2.Domain.Personas;
using Microsoft.EntityFrameworkCore;

namespace ApiPoo2.Infrastructure.Persistence.Repositories;

public sealed class PersonaRepository : GenericRepository<Persona>, IPersonaRepository
{
    public PersonaRepository(AppDbContext dbContext)
        : base(dbContext)
    {
    }

    public Task<Persona?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => DbSet.Include(p => p.Usuario).FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<Persona?> GetByNumeroIdentificacionAsync(
        NumeroIdentificacion numero,
        CancellationToken cancellationToken = default)
        => DbSet.FirstOrDefaultAsync(p => p.NumeroIdentificacion == numero, cancellationToken);

    public Task<Persona?> GetByCorreoAsync(
        CorreoElectronico correo,
        CancellationToken cancellationToken = default)
        => DbSet.FirstOrDefaultAsync(p => p.CorreoElectronico == correo, cancellationToken);

    public Task<bool> ExistsByNumeroIdentificacionAsync(
        NumeroIdentificacion numero,
        CancellationToken cancellationToken = default)
        => DbSet.AsNoTracking().AnyAsync(p => p.NumeroIdentificacion == numero, cancellationToken);

    public async Task<bool> ExistsByLoginAsync(Login login, CancellationToken cancellationToken = default)
    {
        var personaId = await DbSet.AsNoTracking()
            .Where(p => p.Usuario != null && p.Usuario.Login == login)
            .Select(p => p.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return personaId != Guid.Empty;
    }

    public async Task<IReadOnlyDictionary<TipoPersona, int>> CountByTipoAsync(
        CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking()
            .GroupBy(p => p.TipoPersona)
            .Select(g => new { Tipo = g.Key, Cantidad = g.Count() })
            .ToDictionaryAsync(x => x.Tipo, x => x.Cantidad, cancellationToken);
}
