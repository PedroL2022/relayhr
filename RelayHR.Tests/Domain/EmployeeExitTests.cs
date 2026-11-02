using RelayHR.Web.Entities;

namespace RelayHR.Tests.Domain;

/// <summary>HU-01 (checklist) y HU-02 (control de cierre).</summary>
public class EmployeeExitTests
{
    private static EmployeeExit NewExit()
    {
        var exit = new EmployeeExit
        {
            Employee = new Employee { EmployeeId = 1, FullName = "Ana Pérez" },
            ExitDate = new DateOnly(2026, 11, 10)
        };
        exit.GenerateChecklist();
        return exit;
    }

    [Fact]
    public void GenerateChecklist_CreatesTasksForTechnologyHumanResourcesAndFinance()
    {
        var exit = NewExit();

        var areas = exit.Tasks.Select(t => t.Area).Distinct().ToList();

        Assert.Contains(ExitArea.Technology, areas);
        Assert.Contains(ExitArea.HumanResources, areas);
        Assert.Contains(ExitArea.Finance, areas);
        Assert.Equal(ExitChecklistTemplate.Items.Count, exit.Tasks.Count);
    }

    [Fact]
    public void GenerateChecklist_EveryTaskHasResponsibleDueDateAndStartsPending()
    {
        var exit = NewExit();

        Assert.All(exit.Tasks, task =>
        {
            Assert.False(string.IsNullOrWhiteSpace(task.ResponsibleName));
            Assert.True(task.DueDate >= exit.ExitDate);
            Assert.Equal(ExitTaskStatus.Pending, task.Status);
        });
    }

    [Fact]
    public void GenerateChecklist_IncludesEquipmentReturnAndAccessRevocationAsMandatory()
    {
        var exit = NewExit();

        Assert.True(exit.Tasks.Single(t => t.Kind == ExitTaskKind.EquipmentReturn).IsMandatory);
        Assert.True(exit.Tasks.Single(t => t.Kind == ExitTaskKind.AccessRevocation).IsMandatory);
    }

    [Fact]
    public void TryClose_WithPendingMandatoryTasks_IsBlockedAndListsThem()
    {
        var exit = NewExit();

        var closed = exit.TryClose("Admin", DateTime.UtcNow, out var pending);

        Assert.False(closed);
        Assert.Equal(ExitStatus.InProgress, exit.Status);
        Assert.Equal(exit.Tasks.Count(t => t.IsMandatory), pending.Count);
        Assert.Null(exit.ClosedAt);
    }

    [Fact]
    public void TryClose_WithAllMandatoryTasksCompleted_ClosesAndRecordsDateAndUser()
    {
        var exit = NewExit();
        var now = new DateTime(2026, 11, 12, 10, 0, 0, DateTimeKind.Utc);

        foreach (var task in exit.Tasks.Where(t => t.IsMandatory))
        {
            task.SetStatus(ExitTaskStatus.Completed, "Tecnología", now);
        }

        var closed = exit.TryClose("Admin RRHH", now, out var pending);

        Assert.True(closed);
        Assert.Empty(pending);
        Assert.Equal(ExitStatus.Closed, exit.Status);
        Assert.Equal(now, exit.ClosedAt);
        Assert.Equal("Admin RRHH", exit.ClosedBy);
    }

    [Fact]
    public void TryClose_OptionalTasksPending_DoNotBlockClosing()
    {
        var exit = NewExit();
        foreach (var task in exit.Tasks.Where(t => t.IsMandatory))
        {
            task.SetStatus(ExitTaskStatus.Completed, "x", DateTime.UtcNow);
        }

        Assert.Contains(exit.Tasks, t => !t.IsMandatory && t.Status == ExitTaskStatus.Pending);
        Assert.True(exit.TryClose("Admin", DateTime.UtcNow, out _));
    }

    [Fact]
    public void ProgressPercent_ReflectsCompletedTasks()
    {
        var exit = NewExit();
        Assert.Equal(0, exit.ProgressPercent);

        foreach (var task in exit.Tasks)
        {
            task.SetStatus(ExitTaskStatus.Completed, "x", DateTime.UtcNow);
        }

        Assert.Equal(100, exit.ProgressPercent);
    }

    [Fact]
    public void IsKindCompleted_ChangesWhenTheTaskIsCompleted()
    {
        var exit = NewExit();
        Assert.False(exit.IsKindCompleted(ExitTaskKind.AccessRevocation));

        exit.Tasks.Single(t => t.Kind == ExitTaskKind.AccessRevocation)
            .SetStatus(ExitTaskStatus.Completed, "Tecnología", DateTime.UtcNow);

        Assert.True(exit.IsKindCompleted(ExitTaskKind.AccessRevocation));
        Assert.False(exit.IsKindCompleted(ExitTaskKind.EquipmentReturn));
    }
}

public class ExitTaskTests
{
    [Fact]
    public void SetStatus_Completed_RecordsWhoAndWhen()
    {
        var task = new ExitTask();
        var now = new DateTime(2026, 11, 12, 9, 30, 0, DateTimeKind.Utc);

        task.SetStatus(ExitTaskStatus.Completed, "María (Tecnología)", now);

        Assert.Equal(ExitTaskStatus.Completed, task.Status);
        Assert.Equal(now, task.CompletedAt);
        Assert.Equal("María (Tecnología)", task.CompletedBy);
    }

    [Fact]
    public void SetStatus_ReopeningClearsCompletionData()
    {
        var task = new ExitTask();
        task.SetStatus(ExitTaskStatus.Completed, "María", DateTime.UtcNow);

        task.SetStatus(ExitTaskStatus.InProgress, "María", DateTime.UtcNow);

        Assert.Null(task.CompletedAt);
        Assert.Null(task.CompletedBy);
    }

    [Fact]
    public void IsOverdue_OnlyWhenNotCompletedAndPastDueDate()
    {
        var task = new ExitTask { DueDate = new DateOnly(2026, 11, 10) };

        Assert.True(task.IsOverdue(new DateOnly(2026, 11, 11)));
        Assert.False(task.IsOverdue(new DateOnly(2026, 11, 10)));

        task.SetStatus(ExitTaskStatus.Completed, "x", DateTime.UtcNow);
        Assert.False(task.IsOverdue(new DateOnly(2026, 11, 20)));
    }
}
