using RelayHR.Web.Entities;
using RelayHR.Web.Repositories;

namespace RelayHR.Web.Tests.Fakes;

internal sealed class FakeEmployeeExitRepository : IEmployeeExitRepository
{
    public bool EmployeeExists { get; set; } = true;

    public bool HasOpenExit { get; set; }

    public IReadOnlyList<Department> Departments { get; set; } =
    [
        new Department { Id = 1, Name = "Tecnología" },
        new Department { Id = 2, Name = "Recursos Humanos" },
        new Department { Id = 3, Name = "Finanzas" }
    ];

    public HashSet<int> UserIds { get; set; } = [101, 102, 103];

    public List<EmployeeExit> SavedExits { get; } = [];

    public int ChecklistWrites { get; private set; }

    public ExitChecklist? ExistingChecklist { get; set; }

    public Task<bool> EmployeeExistsAsync(int employeeId, CancellationToken cancellationToken) =>
        Task.FromResult(EmployeeExists);

    public Task<bool> HasOpenExitAsync(int employeeId, CancellationToken cancellationToken) =>
        Task.FromResult(HasOpenExit);

    public Task<IReadOnlyList<Department>> GetDepartmentsAsync(
        IReadOnlyCollection<string> departmentNames,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Department>>(
            Departments.Where(department => departmentNames.Contains(
                department.Name,
                StringComparer.OrdinalIgnoreCase)).ToArray());

    public Task<HashSet<int>> GetExistingUserIdsAsync(
        IReadOnlyCollection<int> userIds,
        CancellationToken cancellationToken) =>
        Task.FromResult(UserIds.Intersect(userIds).ToHashSet());

    public Task<int> CreateWithChecklistAsync(
        EmployeeExit employeeExit,
        ExitChecklist checklist,
        CancellationToken cancellationToken)
    {
        employeeExit.Id = SavedExits.Count + 1;
        checklist.Id = SavedExits.Count + 101;
        checklist.EmployeeExitId = employeeExit.Id;
        employeeExit.Checklist = checklist;
        checklist.EmployeeExit = employeeExit;
        SavedExits.Add(employeeExit);
        ExistingChecklist = checklist;
        return Task.FromResult(checklist.Id);
    }

    public Task<ExitChecklist> CreateChecklistIfMissingAsync(
        int employeeExitId,
        ExitChecklist checklist,
        CancellationToken cancellationToken)
    {
        ChecklistWrites++;
        if (ExistingChecklist is not null)
        {
            return Task.FromResult(ExistingChecklist);
        }

        checklist.Id = 201;
        checklist.EmployeeExitId = employeeExitId;
        ExistingChecklist = checklist;
        return Task.FromResult(checklist);
    }

    public Task<ExitChecklist?> GetChecklistAsync(
        int employeeExitId,
        CancellationToken cancellationToken) =>
        Task.FromResult(ExistingChecklist?.EmployeeExitId == employeeExitId
            ? ExistingChecklist
            : null);
}
