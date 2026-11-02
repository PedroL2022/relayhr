using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RelayHR.Web.Data;

namespace RelayHR.Web.Repository;

/// <summary>
/// Implementación base con Entity Framework Core. Cada repositorio concreto hereda de aquí
/// y agrega sus propias consultas.
/// </summary>
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly AppDbContext Db;

    public Repository(AppDbContext db)
    {
        Db = db;
    }

    protected DbSet<T> Set => Db.Set<T>();

    public void Add(T entity) => Set.Add(entity);

    public async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await Set.FindAsync(new object[] { id }, cancellationToken);

    public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) =>
        Set.AnyAsync(predicate, cancellationToken);

    public Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default) =>
        predicate is null ? Set.CountAsync(cancellationToken) : Set.CountAsync(predicate, cancellationToken);
}

/// <summary>Implementa IUnitOfWork sobre el DbContext.</summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;

    public UnitOfWork(AppDbContext db)
    {
        _db = db;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
