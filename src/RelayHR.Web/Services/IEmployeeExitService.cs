using RelayHR.Web.Models;

namespace RelayHR.Web.Services;

public interface IEmployeeExitService
{
    Task<CreateEmployeeExitResponse> CreateAsync(
        CreateEmployeeExitDTO request,
        CancellationToken cancellationToken);

    Task<ExitChecklistResponseDTO?> GetChecklistAsync(
        int employeeExitId,
        CancellationToken cancellationToken);
}
