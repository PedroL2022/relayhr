using RelayHR.Web.Entities;

namespace RelayHR.Web.Repositories;

public interface IEmployeeExitRepository
{
    Task<bool> EmployeeExistsAsync(int employeeId, CancellationToken cancellationToken);

    Task<bool> HasOpenExitAsync(int employeeId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Department>> GetDepartmentsAsync(
        IReadOnlyCollection<string> departmentNames,
        CancellationToken cancellationToken);

    Task<HashSet<int>> GetExistingUserIdsAsync(
        IReadOnlyCollection<int> userIds,
        CancellationToken cancellationToken);

    Task<int> CreateWithChecklistAsync(
        EmployeeExit employeeExit,
        ExitChecklist checklist,
        CancellationToken cancellationToken);

    Task<ExitChecklist> CreateChecklistIfMissingAsync(
        int employeeExitId,
        ExitChecklist checklist,
        CancellationToken cancellationToken);

    Task<ExitChecklist?> GetChecklistAsync(
        int employeeExitId,
        CancellationToken cancellationToken);
}
