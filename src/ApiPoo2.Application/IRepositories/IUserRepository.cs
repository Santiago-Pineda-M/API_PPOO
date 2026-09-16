using ApiPoo2.Domain.Entities;

namespace ApiPoo2.Application.IRepositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<User?> GetByIdWithRefreshTokensAsync(Guid id, CancellationToken cancellationToken = default);
}