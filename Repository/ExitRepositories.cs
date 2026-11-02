using Microsoft.EntityFrameworkCore;
using RelayHR.Web.Data;
using RelayHR.Web.Entities;

namespace RelayHR.Web.Repository;

public interface IExitRepository : IRepository<EmployeeExit>
{
    /// <summary>Todas las salidas, las más recientes primero, con empleado y tareas. Solo lectura.</summary>
    Task<List<EmployeeExit>> GetAllWithTasksAsync(CancellationToken cancellationToken = default);

    /// <summary>Salida con todo lo que muestra su pantalla de detalle. Solo lectura.</summary>
    Task<EmployeeExit?> GetDetailsAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Salida con su empleado. Solo lectura.</summary>
    Task<EmployeeExit?> GetWithEmployeeAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Salida con sus tareas, rastreada para poder cerrarla.</summary>
    Task<EmployeeExit?> GetWithTasksForUpdateAsync(int id, CancellationToken cancellationToken = default);

    Task<int> CountInProgressAsync(CancellationToken cancellationToken = default);
}

public class ExitRepository : Repository<EmployeeExit>, IExitRepository
{
    public ExitRepository(AppDbContext db) : base(db)
    {
    }

    public Task<List<EmployeeExit>> GetAllWithTasksAsync(CancellationToken cancellationToken = default) =>
        Set.AsNoTracking()
            .Include(e => e.Employee)
            .Include(e => e.Tasks)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<EmployeeExit?> GetDetailsAsync(int id, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking()
            .Include(e => e.Employee)
            .Include(e => e.Tasks)
            .Include(e => e.KnowledgeItems)
            .Include(e => e.ExpressVacancy)
            .FirstOrDefaultAsync(e => e.EmployeeExitId == id, cancellationToken);

    public Task<EmployeeExit?> GetWithEmployeeAsync(int id, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking()
            .Include(e => e.Employee)
            .FirstOrDefaultAsync(e => e.EmployeeExitId == id, cancellationToken);

    public Task<EmployeeExit?> GetWithTasksForUpdateAsync(int id, CancellationToken cancellationToken = default) =>
        Set.Include(e => e.Tasks).FirstOrDefaultAsync(e => e.EmployeeExitId == id, cancellationToken);

    public Task<int> CountInProgressAsync(CancellationToken cancellationToken = default) =>
        Set.CountAsync(e => e.Status == ExitStatus.InProgress, cancellationToken);
}

public interface IExitTaskRepository : IRepository<ExitTask>
{
    /// <summary>Tarea con su salida, rastreada para poder cambiar su estado.</summary>
    Task<ExitTask?> GetWithExitAsync(int id, CancellationToken cancellationToken = default);
}

public class ExitTaskRepository : Repository<ExitTask>, IExitTaskRepository
{
    public ExitTaskRepository(AppDbContext db) : base(db)
    {
    }

    public Task<ExitTask?> GetWithExitAsync(int id, CancellationToken cancellationToken = default) =>
        Set.Include(t => t.EmployeeExit).FirstOrDefaultAsync(t => t.ExitTaskId == id, cancellationToken);
}

public interface IKnowledgeItemRepository : IRepository<KnowledgeItem>
{
    /// <summary>Elementos documentados de una salida, con su responsable temporal. Solo lectura.</summary>
    Task<List<KnowledgeItem>> GetByExitAsync(int exitId, CancellationToken cancellationToken = default);

    /// <summary>Elemento con su salida, rastreado para poder cambiar su responsable.</summary>
    Task<KnowledgeItem?> GetWithExitAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Lo que un empleado tiene a cargo como responsable temporal. Solo lectura.</summary>
    Task<List<KnowledgeItem>> GetByAssigneeAsync(int employeeId, CancellationToken cancellationToken = default);

    Task<int> CountUnassignedAsync(CancellationToken cancellationToken = default);
}

public class KnowledgeItemRepository : Repository<KnowledgeItem>, IKnowledgeItemRepository
{
    public KnowledgeItemRepository(AppDbContext db) : base(db)
    {
    }

    public Task<List<KnowledgeItem>> GetByExitAsync(int exitId, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking()
            .Include(k => k.AssignedTo)
            .Where(k => k.EmployeeExitId == exitId)
            .OrderBy(k => k.KnowledgeItemId)
            .ToListAsync(cancellationToken);

    public Task<KnowledgeItem?> GetWithExitAsync(int id, CancellationToken cancellationToken = default) =>
        Set.Include(k => k.EmployeeExit).FirstOrDefaultAsync(k => k.KnowledgeItemId == id, cancellationToken);

    public Task<List<KnowledgeItem>> GetByAssigneeAsync(int employeeId, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking()
            .Include(k => k.EmployeeExit)
                .ThenInclude(e => e.Employee)
            .Where(k => k.AssignedToEmployeeId == employeeId)
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<int> CountUnassignedAsync(CancellationToken cancellationToken = default) =>
        Set.CountAsync(k => k.AssignedToEmployeeId == null, cancellationToken);
}
