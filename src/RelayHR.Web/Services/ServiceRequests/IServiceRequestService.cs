using RelayHR.Web.Models.DTOs.ServiceRequests;

namespace RelayHR.Web.Services.ServiceRequests;

public interface IServiceRequestService
{
    Task<ServiceRequestResult<ServiceRequestCreatedDTO>> CreateAsync(int companyId, CreateServiceRequestDTO dto, CancellationToken ct = default);
    Task<ServiceRequestResult<IReadOnlyList<ServiceRequestResponseDTO>>> GetMineAsync(int userId, bool asFreelancer, CancellationToken ct = default);
    Task<ServiceRequestResult<ServiceRequestResponseDTO>> GetByIdAsync(int userId, bool asFreelancer, int id, CancellationToken ct = default);
    Task<ServiceRequestResult<ServiceRequestRespondedDTO>> RespondAsync(int freelancerUserId, int id, string response, CancellationToken ct = default);
    Task<ServiceRequestResult<ServiceRequestConfirmedDTO>> ConfirmAsync(int companyId, int id, CancellationToken ct = default);
}