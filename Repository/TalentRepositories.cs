using Microsoft.EntityFrameworkCore;
using RelayHR.Web.Data;
using RelayHR.Web.Entities;

namespace RelayHR.Web.Repository;

public interface IFreelancerRepository : IRepository<FreelancerProfile>
{
    /// <summary>Perfil de un usuario, rastreado para poder actualizarlo.</summary>
    Task<FreelancerProfile?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>Perfil de un usuario. Solo lectura.</summary>
    Task<FreelancerProfile?> GetByUserIdReadOnlyAsync(int userId, CancellationToken cancellationToken = default);

    Task<bool> ExistsForUserAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>Perfil con los datos de su usuario. Solo lectura.</summary>
    Task<FreelancerProfile?> GetDetailAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Busca en el directorio (HU-06). Todos los filtros son opcionales.</summary>
    Task<List<FreelancerProfile>> SearchAsync(
        string? category, decimal? maxHourlyRate, bool onlyAvailable, CancellationToken cancellationToken = default);
}

public class FreelancerRepository : Repository<FreelancerProfile>, IFreelancerRepository
{
    public FreelancerRepository(AppDbContext db) : base(db)
    {
    }

    public Task<FreelancerProfile?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default) =>
        Set.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

    public Task<FreelancerProfile?> GetByUserIdReadOnlyAsync(int userId, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

    public Task<bool> ExistsForUserAsync(int userId, CancellationToken cancellationToken = default) =>
        Set.AnyAsync(p => p.UserId == userId, cancellationToken);

    public Task<FreelancerProfile?> GetDetailAsync(int id, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking().Include(p => p.User).FirstOrDefaultAsync(p => p.FreelancerProfileId == id, cancellationToken);

    /// <summary>
    /// Aplica los filtros del directorio. Se combinan (deben cumplirse todos): categoría exacta,
    /// tarifa máxima por hora y solo profesionales disponibles.
    /// </summary>
    public static IQueryable<FreelancerProfile> ApplyFilters(
        IQueryable<FreelancerProfile> query, string? category, decimal? maxHourlyRate, bool onlyAvailable)
    {
        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(p => p.Category == category);
        }

        if (maxHourlyRate.HasValue)
        {
            query = query.Where(p => p.HourlyRate <= maxHourlyRate.Value);
        }

        if (onlyAvailable)
        {
            query = query.Where(p => p.IsAvailable);
        }

        return query;
    }

    public Task<List<FreelancerProfile>> SearchAsync(
        string? category, decimal? maxHourlyRate, bool onlyAvailable, CancellationToken cancellationToken = default)
    {
        var query = Set.AsNoTracking().Include(p => p.User);

        return ApplyFilters(query, category, maxHourlyRate, onlyAvailable)
            .OrderBy(p => p.HourlyRate)
            .ThenBy(p => p.FreelancerProfileId)
            .ToListAsync(cancellationToken);
    }
}

public interface IServiceRequestRepository : IRepository<ServiceRequest>
{
    /// <summary>Solicitudes que envió una empresa (usuario), con el profesional. Solo lectura.</summary>
    Task<List<ServiceRequest>> GetByRequesterAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>Solicitudes que recibió un trabajador independiente (usuario). Solo lectura.</summary>
    Task<List<ServiceRequest>> GetByFreelancerUserAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>Solicitud con el perfil del profesional, rastreada para poder responderla.</summary>
    Task<ServiceRequest?> GetWithProfileAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>True si ya hay una solicitud enviada o aceptada de ese usuario a ese profesional.</summary>
    Task<bool> HasActiveAsync(int freelancerProfileId, int requesterUserId, CancellationToken cancellationToken = default);

    Task<int> CountActiveByRequesterAsync(int userId, CancellationToken cancellationToken = default);

    Task<int> CountSentForFreelancerAsync(int freelancerUserId, CancellationToken cancellationToken = default);
}

public class ServiceRequestRepository : Repository<ServiceRequest>, IServiceRequestRepository
{
    public ServiceRequestRepository(AppDbContext db) : base(db)
    {
    }

    public Task<List<ServiceRequest>> GetByRequesterAsync(int userId, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking()
            .Include(r => r.FreelancerProfile)
                .ThenInclude(p => p.User)
            .Where(r => r.RequestedByUserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<List<ServiceRequest>> GetByFreelancerUserAsync(int userId, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking()
            .Include(r => r.RequestedBy)
            .Where(r => r.FreelancerProfile.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<ServiceRequest?> GetWithProfileAsync(int id, CancellationToken cancellationToken = default) =>
        Set.Include(r => r.FreelancerProfile).FirstOrDefaultAsync(r => r.ServiceRequestId == id, cancellationToken);

    public Task<bool> HasActiveAsync(int freelancerProfileId, int requesterUserId, CancellationToken cancellationToken = default) =>
        Set.AnyAsync(r =>
            r.FreelancerProfileId == freelancerProfileId &&
            r.RequestedByUserId == requesterUserId &&
            (r.Status == ServiceRequestStatus.Sent || r.Status == ServiceRequestStatus.Accepted),
            cancellationToken);

    public Task<int> CountActiveByRequesterAsync(int userId, CancellationToken cancellationToken = default) =>
        Set.CountAsync(r =>
            r.RequestedByUserId == userId &&
            (r.Status == ServiceRequestStatus.Sent || r.Status == ServiceRequestStatus.Accepted),
            cancellationToken);

    public Task<int> CountSentForFreelancerAsync(int freelancerUserId, CancellationToken cancellationToken = default) =>
        Set.CountAsync(r =>
            r.FreelancerProfile.UserId == freelancerUserId && r.Status == ServiceRequestStatus.Sent,
            cancellationToken);
}
