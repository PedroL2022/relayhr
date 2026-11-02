using Microsoft.EntityFrameworkCore;
using RelayHR.Web.Data;
using RelayHR.Web.Entities;

namespace RelayHR.Web.Repository;

public interface IVacancyRepository : IRepository<ExpressVacancy>
{
    /// <summary>Bandeja del reclutador: las más recientes primero y, si se indica, solo con ese estado. Solo lectura.</summary>
    Task<List<ExpressVacancy>> GetInboxAsync(VacancyStatus? status, CancellationToken cancellationToken = default);

    /// <summary>Vacante con departamento, salida y empleado. Solo lectura.</summary>
    Task<ExpressVacancy?> GetDetailAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Vacante con su salida, rastreada para poder publicarla.</summary>
    Task<ExpressVacancy?> GetWithExitForUpdateAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Vacantes visibles para los candidatos.</summary>
    Task<List<ExpressVacancy>> GetPublishedAsync(CancellationToken cancellationToken = default);

    Task<ExpressVacancy?> GetPublishedByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<int> CountNotPublishedAsync(CancellationToken cancellationToken = default);
}

public class VacancyRepository : Repository<ExpressVacancy>, IVacancyRepository
{
    public VacancyRepository(AppDbContext db) : base(db)
    {
    }

    public async Task<List<ExpressVacancy>> GetInboxAsync(VacancyStatus? status, CancellationToken cancellationToken = default)
    {
        var query = Set.AsNoTracking()
            .Include(v => v.Department)
            .Include(v => v.EmployeeExit)
                .ThenInclude(e => e.Employee)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(v => v.Status == status.Value);
        }

        return await query.OrderByDescending(v => v.CreatedAt).ToListAsync(cancellationToken);
    }

    public Task<ExpressVacancy?> GetDetailAsync(int id, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking()
            .Include(v => v.Department)
            .Include(v => v.EmployeeExit)
                .ThenInclude(e => e.Employee)
            .FirstOrDefaultAsync(v => v.ExpressVacancyId == id, cancellationToken);

    public Task<ExpressVacancy?> GetWithExitForUpdateAsync(int id, CancellationToken cancellationToken = default) =>
        Set.Include(v => v.EmployeeExit).FirstOrDefaultAsync(v => v.ExpressVacancyId == id, cancellationToken);

    public Task<List<ExpressVacancy>> GetPublishedAsync(CancellationToken cancellationToken = default) =>
        Set.AsNoTracking()
            .Include(v => v.Department)
            .Where(v => v.Status == VacancyStatus.Published)
            .OrderByDescending(v => v.PublishedAt)
            .ToListAsync(cancellationToken);

    public Task<ExpressVacancy?> GetPublishedByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking()
            .Include(v => v.Department)
            .FirstOrDefaultAsync(v => v.ExpressVacancyId == id && v.Status == VacancyStatus.Published, cancellationToken);

    public Task<int> CountNotPublishedAsync(CancellationToken cancellationToken = default) =>
        Set.CountAsync(v => v.Status != VacancyStatus.Published, cancellationToken);
}
