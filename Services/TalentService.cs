using RelayHR.Web.Entities;
using RelayHR.Web.Repository;
using RelayHR.Web.Security;

namespace RelayHR.Web.Services;

/// <summary>
/// Directorio de talento interino: perfiles de trabajadores independientes (HU-06)
/// y solicitudes de servicio (HU-07).
/// </summary>
public class TalentService
{
    private readonly IFreelancerRepository _profiles;
    private readonly IServiceRequestRepository _requests;
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _time;

    public TalentService(
        IFreelancerRepository profiles,
        IServiceRequestRepository requests,
        IUserRepository users,
        IUnitOfWork unitOfWork,
        TimeProvider time)
    {
        _profiles = profiles;
        _requests = requests;
        _users = users;
        _unitOfWork = unitOfWork;
        _time = time;
    }

    // ---------- Perfil del trabajador independiente ----------

    /// <summary>Crea el perfil del usuario o actualiza el que ya tiene.</summary>
    public async Task<Result> SaveProfileAsync(
        int userId, string category, int experienceYears, string experienceSummary,
        string? portfolioUrl, decimal hourlyRate, bool isAvailable, string? availabilityNote,
        CancellationToken cancellationToken = default)
    {
        if (!FreelancerCategories.All.Contains(category))
        {
            return Result.Fail("La categoría seleccionada no es válida.");
        }

        var user = await _users.GetByIdAsync(userId, cancellationToken);
        if (user is null || user.Role != Roles.Freelancer)
        {
            return Result.Fail("Solo los trabajadores independientes pueden tener un perfil.");
        }

        var profile = await _profiles.GetByUserIdAsync(userId, cancellationToken);

        if (profile is null)
        {
            profile = new FreelancerProfile { UserId = userId };
            _profiles.Add(profile);
        }

        profile.Apply(category, experienceYears, experienceSummary, portfolioUrl,
            hourlyRate, isAvailable, availabilityNote, _time.GetUtcNow().UtcDateTime);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    // ---------- Búsqueda (HU-06) ----------

    public Task<List<FreelancerProfile>> SearchAsync(
        string? category, decimal? maxHourlyRate, bool onlyAvailable,
        CancellationToken cancellationToken = default) =>
        _profiles.SearchAsync(category, maxHourlyRate, onlyAvailable, cancellationToken);

    // ---------- Solicitudes de servicio (HU-07) ----------

    /// <summary>
    /// Registra una solicitud con un solo clic. Los datos de la empresa se toman de la cuenta
    /// del usuario que la envía.
    /// </summary>
    public async Task<Result<int>> RequestServiceAsync(
        int freelancerProfileId, int requesterUserId, CancellationToken cancellationToken = default)
    {
        var profile = await _profiles.GetByIdAsync(freelancerProfileId, cancellationToken);

        if (profile is null)
        {
            return Result<int>.Fail("El perfil no existe.");
        }

        var requester = await _users.GetByIdAsync(requesterUserId, cancellationToken);

        if (requester is null || !Roles.IsCompanyRole(requester.Role))
        {
            return Result<int>.Fail("Solo los usuarios de empresa pueden solicitar servicios.");
        }

        if (!profile.IsAvailable)
        {
            return Result<int>.Fail("Este profesional no está disponible por ahora.");
        }

        if (await _requests.HasActiveAsync(freelancerProfileId, requesterUserId, cancellationToken))
        {
            return Result<int>.Fail("Ya tienes una solicitud activa con este profesional.");
        }

        var request = new ServiceRequest
        {
            FreelancerProfileId = freelancerProfileId,
            RequestedByUserId = requesterUserId,
            CompanyName = string.IsNullOrWhiteSpace(requester.CompanyName) ? requester.FullName : requester.CompanyName,
            Status = ServiceRequestStatus.Sent,
            CreatedAt = _time.GetUtcNow().UtcDateTime
        };

        _requests.Add(request);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<int>.Ok(request.ServiceRequestId);
    }

    /// <summary>El profesional acepta o rechaza una solicitud que recibió.</summary>
    public async Task<Result> RespondAsync(
        int requestId, int freelancerUserId, bool accept, CancellationToken cancellationToken = default)
    {
        var request = await _requests.GetWithProfileAsync(requestId, cancellationToken);

        if (request is null || request.FreelancerProfile.UserId != freelancerUserId)
        {
            return Result.Fail("La solicitud no existe.");
        }

        if (!request.Respond(accept, _time.GetUtcNow().UtcDateTime))
        {
            return Result.Fail("Esta solicitud ya fue respondida.");
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    /// <summary>La empresa confirma el servicio una vez que el profesional aceptó.</summary>
    public async Task<Result> ConfirmAsync(
        int requestId, int requesterUserId, CancellationToken cancellationToken = default)
    {
        var request = await _requests.GetByIdAsync(requestId, cancellationToken);

        if (request is null || request.RequestedByUserId != requesterUserId)
        {
            return Result.Fail("La solicitud no existe.");
        }

        if (!request.Confirm(_time.GetUtcNow().UtcDateTime))
        {
            return Result.Fail("Solo se pueden confirmar las solicitudes que el profesional aceptó.");
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}
