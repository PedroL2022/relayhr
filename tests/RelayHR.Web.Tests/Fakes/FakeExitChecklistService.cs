using RelayHR.Web.Entities;
using RelayHR.Web.Services;

namespace RelayHR.Web.Tests.Fakes;

internal sealed class FakeExitChecklistService : IExitChecklistService
{
    public int GenerationCount { get; private set; }

    public Task<ExitChecklist> GenerateAsync(
        EmployeeExit employeeExit,
        CancellationToken cancellationToken)
    {
        GenerationCount++;
        return Task.FromResult(new ExitChecklist
        {
            Tasks =
            [
                new ChecklistTask
                {
                    DepartmentId = 1,
                    ResponsibleUserId = 101,
                    Title = "Desactivar accesos",
                    DueDate = employeeExit.EffectiveDate.AddDays(-1),
                    Status = ChecklistTaskStatus.Pending
                }
            ]
        });
    }
}
