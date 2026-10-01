using ApiPoo2.Application.IRepositories;
using ApiPoo2.Domain.Personas;
using ApiPoo2.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace ApiPoo2.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly AppDbContext _dbContext;

    public UserRepository(AppDbContext dbContext) => _dbContext = dbContext;

    public Task<User?> GetByPersonaAndLoginAsync(
        Guid idPersona,
        Login login,
        CancellationToken cancellationToken = default)
        => _dbContext.Users
            .Include(u => u.RefreshTokensVisibles)
            .AsSplitQuery()
            .FirstOrDefaultAsync(u => u.IdPersona == idPersona && u.Login == login, cancellationToken);

    public Task<User?> GetByLoginAsync(Login login, CancellationToken cancellationToken = default)
        => _dbContext.Users
            .Include(u => u.RefreshTokensVisibles)
            .AsSplitQuery()
            .FirstOrDefaultAsync(u => u.Login == login, cancellationToken);

    public Task<Persona?> GetPersonaByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.Personas
            .Include(p => p.Usuario)
            .ThenInclude(u => u.RefreshTokensVisibles)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<User?> GetByApiKeyAsync(ApiKey apiKey, CancellationToken cancellationToken = default)
        => _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.ApiKey == apiKey, cancellationToken);

    public async Task<IReadOnlyList<User>> GetByPersonaAsync(
        Guid idPersona,
        CancellationToken cancellationToken = default)
        => await _dbContext.Users
            .Where(u => u.IdPersona == idPersona)
            .ToListAsync(cancellationToken);

    public void Add(User user) => _dbContext.Users.Add(user);
}
