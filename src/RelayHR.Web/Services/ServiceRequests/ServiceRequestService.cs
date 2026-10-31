using Microsoft.EntityFrameworkCore;
using RelayHR.Web.Data.ServiceRequests;
using RelayHR.Web.Entities;
using RelayHR.Web.Models.DTOs.ServiceRequests;

namespace RelayHR.Web.Services.ServiceRequests;

public class ServiceRequestService : IServiceRequestService
{
    private readonly IServiceRequestDbContext _db;

    public ServiceRequestService(IServiceRequestDbContext db) => _db = db;

    public async Task<ServiceRequestResult<ServiceRequestCreatedDTO>> CreateAsync(
        int companyId, CreateServiceRequestDTO dto, CancellationToken ct = default)
    {
        if (dto.FreelancerProfileId <= 0 || string.IsNullOrWhiteSpace(dto.Description))
            return ServiceRequestResult<ServiceRequestCreatedDTO>.Fail(
                ServiceRequestResultStatus.BadRequest, "El profesional y la descripción del servicio son obligatorios.");

        var profile = await _db.FreelancerProfiles.AsNoTracking()
            .Where(p => p.Id == dto.FreelancerProfileId)
            .Select(p => new { p.IsActive })
            .FirstOrDefaultAsync(ct);

        if (profile is null)
            return ServiceRequestResult<ServiceRequestCreatedDTO>.Fail(
                ServiceRequestResultStatus.NotFound, "El profesional solicitado no existe.");

        if (!profile.IsActive)
            return ServiceRequestResult<ServiceRequestCreatedDTO>.Fail(
                ServiceRequestResultStatus.BadRequest, "El profesional no tiene un perfil activo.");

        if (await HasActiveRequestAsync(companyId, dto.FreelancerProfileId, ct))
            return DuplicateResult();

        var entity = new ServiceRequest
        {
            CompanyId = companyId,
            FreelancerProfileId = dto.FreelancerProfileId,
            Description = dto.Description.Trim(),
            Status = ServiceRequestStatus.Sent,
            CreatedAt = DateTime.UtcNow
        };

        _db.ServiceRequests.Add(entity);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Carrera entre dos clics simultáneos: el índice único filtrado detuvo al segundo.
            if (await HasActiveRequestAsync(companyId, dto.FreelancerProfileId, ct))
                return DuplicateResult();
            throw;
        }

        return ServiceRequestResult<ServiceRequestCreatedDTO>.Created(new ServiceRequestCreatedDTO
        {
            ServiceRequestId = entity.ServiceRequestId,
            CompanyId = entity.CompanyId,
            FreelancerProfileId = entity.FreelancerProfileId,
            Status = entity.Status.ToString(),
            CreatedAt = entity.CreatedAt
        }, "Solicitud de servicio registrada correctamente.");
    }

    public async Task<ServiceRequestResult<IReadOnlyList<ServiceRequestResponseDTO>>> GetMineAsync(
        int userId, bool asFreelancer, CancellationToken ct = default)
    {
        IQueryable<ServiceRequest> query = _db.ServiceRequests.AsNoTracking();

        query = asFreelancer
            ? query.Where(r => r.FreelancerProfile!.UserId == userId)
            : query.Where(r => r.CompanyId == userId);

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.ServiceRequestId)
            .ToListAsync(ct);

        IReadOnlyList<ServiceRequestResponseDTO> data = items.Select(ToDto).ToList();
        return ServiceRequestResult<IReadOnlyList<ServiceRequestResponseDTO>>.Ok(
            data, "Solicitudes obtenidas correctamente.");
    }

    public async Task<ServiceRequestResult<ServiceRequestResponseDTO>> GetByIdAsync(
        int userId, bool asFreelancer, int id, CancellationToken ct = default)
    {
        var request = await LoadWithProfileAsync(id, ct);

        // Si no existe o es ajena se responde igual (404) para no revelar qué ids existen.
        var isOwner = request is not null &&
            (asFreelancer ? request.FreelancerProfile?.UserId == userId : request.CompanyId == userId);

        if (!isOwner)
            return ServiceRequestResult<ServiceRequestResponseDTO>.Fail(
                ServiceRequestResultStatus.NotFound, "La solicitud de servicio no existe.");

        return ServiceRequestResult<ServiceRequestResponseDTO>.Ok(
            ToDto(request!), "Solicitud obtenida correctamente.");
    }

    public async Task<ServiceRequestResult<ServiceRequestRespondedDTO>> RespondAsync(
        int freelancerUserId, int id, string response, CancellationToken ct = default)
    {
        ServiceRequestStatus target;
        var normalized = response?.Trim();

        if (string.Equals(normalized, "Accepted", StringComparison.OrdinalIgnoreCase))
            target = ServiceRequestStatus.Accepted;
        else if (string.Equals(normalized, "Rejected", StringComparison.OrdinalIgnoreCase))
            target = ServiceRequestStatus.Rejected;
        else
            return ServiceRequestResult<ServiceRequestRespondedDTO>.Fail(
                ServiceRequestResultStatus.BadRequest, "La respuesta debe ser 'Accepted' o 'Rejected'.");

        var request = await LoadWithProfileAsync(id, ct);
        if (request is null)
            return ServiceRequestResult<ServiceRequestRespondedDTO>.Fail(
                ServiceRequestResultStatus.NotFound, "La solicitud de servicio no existe.");

        if (request.FreelancerProfile is null || request.FreelancerProfile.UserId != freelancerUserId)
            return ServiceRequestResult<ServiceRequestRespondedDTO>.Fail(
                ServiceRequestResultStatus.Forbidden, "Solo el profesional destinatario puede responder esta solicitud.");

        if (request.Status != ServiceRequestStatus.Sent)
            return InvalidTransition<ServiceRequestRespondedDTO>(request.Status);

        // UPDATE atómico condicionado al estado: si otra petición cambió la fila antes, afecta 0 filas.
        var now = DateTime.UtcNow;
        var rows = await _db.ServiceRequests
            .Where(r => r.ServiceRequestId == id && r.Status == ServiceRequestStatus.Sent)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.Status, target)
                .SetProperty(r => r.RespondedAt, (DateTime?)now), ct);

        if (rows == 0)
            return InvalidTransition<ServiceRequestRespondedDTO>(null);

        return ServiceRequestResult<ServiceRequestRespondedDTO>.Ok(new ServiceRequestRespondedDTO
        {
            ServiceRequestId = id,
            Status = target.ToString(),
            RespondedAt = now
        }, "Solicitud respondida correctamente.");
    }

    public async Task<ServiceRequestResult<ServiceRequestConfirmedDTO>> ConfirmAsync(
        int companyId, int id, CancellationToken ct = default)
    {
        var request = await LoadWithProfileAsync(id, ct);
        if (request is null)
            return ServiceRequestResult<ServiceRequestConfirmedDTO>.Fail(
                ServiceRequestResultStatus.NotFound, "La solicitud de servicio no existe.");

        if (request.CompanyId != companyId)
            return ServiceRequestResult<ServiceRequestConfirmedDTO>.Fail(
                ServiceRequestResultStatus.Forbidden, "Solo la empresa solicitante puede confirmar el servicio.");

        if (request.Status != ServiceRequestStatus.Accepted)
            return InvalidTransition<ServiceRequestConfirmedDTO>(request.Status);

        var now = DateTime.UtcNow;
        var rows = await _db.ServiceRequests
            .Where(r => r.ServiceRequestId == id && r.Status == ServiceRequestStatus.Accepted)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.Status, ServiceRequestStatus.Confirmed)
                .SetProperty(r => r.ConfirmedAt, (DateTime?)now), ct);

        if (rows == 0)
            return InvalidTransition<ServiceRequestConfirmedDTO>(null);

        return ServiceRequestResult<ServiceRequestConfirmedDTO>.Ok(new ServiceRequestConfirmedDTO
        {
            ServiceRequestId = id,
            Status = ServiceRequestStatus.Confirmed.ToString(),
            ConfirmedAt = now
        }, "Servicio confirmado correctamente.");
    }

    // ----------------------------------------------------------------- helpers

    private Task<bool> HasActiveRequestAsync(int companyId, int freelancerProfileId, CancellationToken ct) =>
        _db.ServiceRequests.AnyAsync(r =>
            r.CompanyId == companyId &&
            r.FreelancerProfileId == freelancerProfileId &&
            (r.Status == ServiceRequestStatus.Sent || r.Status == ServiceRequestStatus.Accepted), ct);

    private Task<ServiceRequest?> LoadWithProfileAsync(int id, CancellationToken ct) =>
        _db.ServiceRequests.AsNoTracking()
            .Include(r => r.FreelancerProfile)
            .FirstOrDefaultAsync(r => r.ServiceRequestId == id, ct);

    private static ServiceRequestResult<ServiceRequestCreatedDTO> DuplicateResult() =>
        ServiceRequestResult<ServiceRequestCreatedDTO>.Fail(
            ServiceRequestResultStatus.Conflict,
            "Ya existe una solicitud activa (enviada o aceptada) de tu empresa para este profesional.");

    private static ServiceRequestResult<T> InvalidTransition<T>(ServiceRequestStatus? current) =>
        ServiceRequestResult<T>.Fail(
            ServiceRequestResultStatus.Conflict,
            current is null
                ? "La solicitud cambió de estado mientras se procesaba la operación."
                : $"Transición no permitida: la solicitud está en estado {current}.");

    private static ServiceRequestResponseDTO ToDto(ServiceRequest r) => new()
    {
        ServiceRequestId = r.ServiceRequestId,
        CompanyId = r.CompanyId,
        FreelancerProfileId = r.FreelancerProfileId,
        Description = r.Description,
        Status = r.Status.ToString(),
        CreatedAt = r.CreatedAt,
        RespondedAt = r.RespondedAt,
        ConfirmedAt = r.ConfirmedAt
    };
}