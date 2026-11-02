using Microsoft.EntityFrameworkCore;
using RelayHR.Web.Data;
using RelayHR.Web.Entities;

namespace RelayHR.Web.Repository;

public interface IDepartmentRepository : IRepository<Department>
{
    Task<List<Department>> GetAllOrderedAsync(CancellationToken cancellationToken = default);
}

public class DepartmentRepository : Repository<Department>, IDepartmentRepository
{
    public DepartmentRepository(AppDbContext db) : base(db)
    {
    }

    public Task<List<Department>> GetAllOrderedAsync(CancellationToken cancellationToken = default) =>
        Set.AsNoTracking().OrderBy(d => d.Name).ToListAsync(cancellationToken);
}

public interface IPositionRepository : IRepository<Position>
{
    Task<List<Position>> GetAllWithDepartmentAsync(CancellationToken cancellationToken = default);
}

public class PositionRepository : Repository<Position>, IPositionRepository
{
    public PositionRepository(AppDbContext db) : base(db)
    {
    }

    public Task<List<Position>> GetAllWithDepartmentAsync(CancellationToken cancellationToken = default) =>
        Set.AsNoTracking().Include(p => p.Department).OrderBy(p => p.Title).ToListAsync(cancellationToken);
}

public interface IEmployeeRepository : IRepository<Employee>
{
    Task<List<Employee>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default);

    /// <summary>Empleados activos ordenados por nombre, sin incluir (opcionalmente) a uno.</summary>
    Task<List<Employee>> GetActiveAsync(int? exceptEmployeeId = null, CancellationToken cancellationToken = default);

    /// <summary>Empleado con su puesto, rastreado para poder modificarlo (por ejemplo, desactivarlo).</summary>
    Task<Employee?> GetWithPositionAsync(int id, CancellationToken cancellationToken = default);

    Task<Employee?> GetReadOnlyAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> IsActiveAsync(int id, CancellationToken cancellationToken = default);
}

public class EmployeeRepository : Repository<Employee>, IEmployeeRepository
{
    public EmployeeRepository(AppDbContext db) : base(db)
    {
    }

    public Task<List<Employee>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default) =>
        Set.AsNoTracking()
            .Include(e => e.Position)
            .Include(e => e.Department)
            .OrderBy(e => e.FullName)
            .ToListAsync(cancellationToken);

    public Task<List<Employee>> GetActiveAsync(int? exceptEmployeeId = null, CancellationToken cancellationToken = default)
    {
        var query = Set.AsNoTracking().Where(e => e.IsActive);

        if (exceptEmployeeId.HasValue)
        {
            query = query.Where(e => e.EmployeeId != exceptEmployeeId.Value);
        }

        return query.OrderBy(e => e.FullName).ToListAsync(cancellationToken);
    }

    public Task<Employee?> GetWithPositionAsync(int id, CancellationToken cancellationToken = default) =>
        Set.Include(e => e.Position).FirstOrDefaultAsync(e => e.EmployeeId == id, cancellationToken);

    public Task<Employee?> GetReadOnlyAsync(int id, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking().FirstOrDefaultAsync(e => e.EmployeeId == id, cancellationToken);

    public Task<bool> IsActiveAsync(int id, CancellationToken cancellationToken = default) =>
        Set.AnyAsync(e => e.EmployeeId == id && e.IsActive, cancellationToken);
}
