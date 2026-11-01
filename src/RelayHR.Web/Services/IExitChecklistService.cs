using RelayHR.Web.Entities;

namespace RelayHR.Web.Services;

public interface IExitChecklistService
{
    Task<ExitChecklist> GenerateAsync(
        EmployeeExit employeeExit,
        CancellationToken cancellationToken);
}
