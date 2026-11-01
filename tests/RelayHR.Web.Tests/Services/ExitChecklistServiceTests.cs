using Microsoft.Extensions.Options;
using RelayHR.Web.Entities;
using RelayHR.Web.Services;
using RelayHR.Web.Tests.Fakes;

namespace RelayHR.Web.Tests.Services;

public sealed class ExitChecklistServiceTests
{
    [Fact]
    public async Task GenerateAsync_creates_pending_tasks_with_departments_responsibles_and_due_dates()
    {
        var repository = new FakeEmployeeExitRepository();
        var service = new ExitChecklistService(repository, Options.Create(CreateOptions()));
        var effectiveDate = new DateOnly(2026, 12, 15);
        var employeeExit = new EmployeeExit
        {
            Id = 15,
            EmployeeId = 42,
            EffectiveDate = effectiveDate
        };

        var checklist = await service.GenerateAsync(employeeExit, CancellationToken.None);

        Assert.Equal(6, checklist.Tasks.Count);
        Assert.Equal(
            new[] { "Finanzas", "Recursos Humanos", "Tecnología" },
            checklist.Tasks.Select(task => task.Department.Name).Distinct().Order().ToArray());
        Assert.All(checklist.Tasks, task => Assert.Equal(ChecklistTaskStatus.Pending, task.Status));
        Assert.All(checklist.Tasks, task => Assert.True(task.ResponsibleUserId > 0));
        Assert.Equal(
            effectiveDate.AddDays(-2),
            checklist.Tasks.Single(task => task.Title == "Recuperar equipos").DueDate);
        Assert.All(
            checklist.Tasks.Where(task => task.Department.Name == "Recursos Humanos"),
            task => Assert.Equal(102, task.ResponsibleUserId));
    }

    [Fact]
    public async Task GenerateAsync_returns_an_existing_checklist_without_creating_another()
    {
        var existingChecklist = new ExitChecklist
        {
            Id = 70,
            EmployeeExitId = 15,
            Tasks =
            [
                new ChecklistTask
                {
                    Id = 80,
                    DepartmentId = 1,
                    ResponsibleUserId = 101,
                    Title = "Tarea existente",
                    DueDate = new DateOnly(2026, 12, 14),
                    Status = ChecklistTaskStatus.Pending
                }
            ]
        };
        var repository = new FakeEmployeeExitRepository { ExistingChecklist = existingChecklist };
        var service = new ExitChecklistService(repository, Options.Create(CreateOptions()));

        var checklist = await service.GenerateAsync(
            new EmployeeExit { Id = 15, EffectiveDate = new DateOnly(2026, 12, 15) },
            CancellationToken.None);

        Assert.Same(existingChecklist, checklist);
        Assert.Equal(0, repository.ChecklistWrites);
    }

    [Fact]
    public async Task GenerateAsync_fails_when_a_department_has_no_responsible_user()
    {
        var configuration = CreateOptions();
        configuration.ResponsibleUserIds.Remove("Finance");
        var service = new ExitChecklistService(
            new FakeEmployeeExitRepository(),
            Options.Create(configuration));

        await Assert.ThrowsAsync<ChecklistConfigurationException>(() =>
            service.GenerateAsync(
                new EmployeeExit { Id = 15, EffectiveDate = new DateOnly(2026, 12, 15) },
                CancellationToken.None));
    }

    private static ChecklistOptions CreateOptions() => new()
    {
        ResponsibleUserIds = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["Technology"] = 101,
            ["HumanResources"] = 102,
            ["Finance"] = 103
        },
        Tasks =
        [
            new ChecklistTaskTemplate
            {
                Department = "Tecnología",
                Title = "Desactivar accesos",
                DaysBeforeEffectiveDate = 3
            },
            new ChecklistTaskTemplate
            {
                Department = "Tecnología",
                Title = "Recuperar equipos",
                DaysBeforeEffectiveDate = 2
            },
            new ChecklistTaskTemplate
            {
                Department = "Recursos Humanos",
                Title = "Preparar documentación",
                DaysBeforeEffectiveDate = 0
            },
            new ChecklistTaskTemplate
            {
                Department = "Recursos Humanos",
                Title = "Realizar entrevista",
                DaysBeforeEffectiveDate = 0
            },
            new ChecklistTaskTemplate
            {
                Department = "Finanzas",
                Title = "Calcular liquidación",
                DaysBeforeEffectiveDate = 2
            },
            new ChecklistTaskTemplate
            {
                Department = "Finanzas",
                Title = "Verificar obligaciones",
                DaysBeforeEffectiveDate = 1
            }
        ]
    };
}
