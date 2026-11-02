using Microsoft.EntityFrameworkCore;
using RelayHR.Web.Data;
using RelayHR.Web.Entities;

namespace RelayHR.Web.Repository;

public interface IUserRepository : IRepository<User>
{
    /// <summary>Busca por correo (ya normalizado en minúsculas). Solo lectura.</summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
}

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(AppDbContext db) : base(db)
    {
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default) =>
        Set.AnyAsync(u => u.Email == email, cancellationToken);
}
