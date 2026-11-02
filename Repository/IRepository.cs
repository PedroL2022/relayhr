using System.Linq.Expressions;

namespace RelayHR.Web.Repository;

/// <summary>
/// Operaciones básicas que comparten todos los repositorios.
/// Un repositorio esconde cómo se consulta la base de datos: quien lo usa (servicios y
/// controladores) pide lo que necesita con un método con nombre claro y no escribe consultas.
/// </summary>
public interface IRepository<T> where T : class
{
    /// <summary>Marca la entidad como nueva. Se guarda al llamar a IUnitOfWork.SaveChangesAsync.</summary>
    void Add(T entity);

    /// <summary>Busca por clave primaria. La entidad queda rastreada, así que se puede modificar y guardar.</summary>
    Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Unidad de trabajo: guarda en una sola transacción todo lo que los repositorios agregaron o modificaron.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
