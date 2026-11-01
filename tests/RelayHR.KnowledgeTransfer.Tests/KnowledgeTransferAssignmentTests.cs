using RelayHR.Web.Data.KnowledgeTransfer;
using RelayHR.Web.DTOs.KnowledgeTransfer;
using RelayHR.Web.Services.KnowledgeTransfer;
using Xunit;

namespace RelayHR.KnowledgeTransfer.Tests;

// Pruebas unitarias: asignación (SCRUM-26) y seguimiento (SCRUM-27).
public class KnowledgeTransferAssignmentTests
{
    private static async Task<int> AddTaskAsync(KnowledgeTransferService service, int exitId, string title = "Tarea")
    {
        var result = await service.CreateTaskAsync(exitId, new CreateKnowledgeTransferTaskDTO
        {
            Title = title,
            Description = "Descripcion",
            Status = "Pending"
        });
        return result.Data!.Id;
    }

    private static AssignTemporaryResponsibleDTO To(int userId) => new() { TemporaryResponsibleUserId = userId };

    [Fact]
    public async Task Assign_WithoutCaller_ReturnsUnauthorized()
    {
        await using var db = TestHelpers.CreateContext();
        var service = new KnowledgeTransferService(db);

        var result = await service.AssignTemporaryResponsibleAsync(1, null, To(1));

        Assert.Equal(401, result.StatusCode);
    }

    [Fact]
    public async Task Assign_TaskNotFound_ReturnsNotFound()
    {
        await using var db = TestHelpers.CreateContext();
        var user = await TestHelpers.AddUserAsync(db);
        var service = new KnowledgeTransferService(db);

        var result = await service.AssignTemporaryResponsibleAsync(999, user.Id, To(user.Id));

        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task Assign_UnknownUser_ReturnsBadRequest()
    {
        await using var db = TestHelpers.CreateContext();
        var exit = await TestHelpers.AddExitAsync(db);
        var caller = await TestHelpers.AddUserAsync(db);
        var service = new KnowledgeTransferService(db);
        var taskId = await AddTaskAsync(service, exit.Id);

        var result = await service.AssignTemporaryResponsibleAsync(taskId, caller.Id, To(999));

        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task Assign_InactiveUser_ReturnsBadRequest()
    {
        await using var db = TestHelpers.CreateContext();
        var exit = await TestHelpers.AddExitAsync(db);
        var caller = await TestHelpers.AddUserAsync(db);
        var inactive = await TestHelpers.AddUserAsync(db, isActive: false);
        var service = new KnowledgeTransferService(db);
        var taskId = await AddTaskAsync(service, exit.Id);

        var result = await service.AssignTemporaryResponsibleAsync(taskId, caller.Id, To(inactive.Id));

        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task Assign_MissingUserId_ReturnsBadRequest()
    {
        await using var db = TestHelpers.CreateContext();
        var exit = await TestHelpers.AddExitAsync(db);
        var caller = await TestHelpers.AddUserAsync(db);
        var service = new KnowledgeTransferService(db);
        var taskId = await AddTaskAsync(service, exit.Id);

        var result = await service.AssignTemporaryResponsibleAsync(
            taskId, caller.Id, new AssignTemporaryResponsibleDTO());

        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task Assign_ThenReassign_KeepsASingleResponsible()
    {
        await using var db = TestHelpers.CreateContext();
        var exit = await TestHelpers.AddExitAsync(db);
        var first = await TestHelpers.AddUserAsync(db);
        var second = await TestHelpers.AddUserAsync(db);
        var service = new KnowledgeTransferService(db);
        var taskId = await AddTaskAsync(service, exit.Id);

        var assigned = await service.AssignTemporaryResponsibleAsync(taskId, first.Id, To(first.Id));
        var reassigned = await service.AssignTemporaryResponsibleAsync(taskId, first.Id, To(second.Id));

        Assert.Equal(200, assigned.StatusCode);
        Assert.Equal(200, reassigned.StatusCode);

        var mineFirst = await service.GetMyTasksAsync(first.Id);
        var mineSecond = await service.GetMyTasksAsync(second.Id);
        Assert.Empty(mineFirst.Data!);
        Assert.Single(mineSecond.Data!);
    }

    [Fact]
    public async Task GetMyTasks_WithoutUser_ReturnsUnauthorized()
    {
        await using var db = TestHelpers.CreateContext();
        var service = new KnowledgeTransferService(db);

        var result = await service.GetMyTasksAsync(null);

        Assert.Equal(401, result.StatusCode);
    }

    [Fact]
    public async Task GetMyTasks_ReturnsOnlyTasksAssignedToThatUser()
    {
        await using var db = TestHelpers.CreateContext();
        var exit = await TestHelpers.AddExitAsync(db);
        var me = await TestHelpers.AddUserAsync(db);
        var other = await TestHelpers.AddUserAsync(db);
        var service = new KnowledgeTransferService(db);
        var mine = await AddTaskAsync(service, exit.Id, "Mia");
        var theirs = await AddTaskAsync(service, exit.Id, "Ajena");
        await AddTaskAsync(service, exit.Id, "Sin asignar");
        await service.AssignTemporaryResponsibleAsync(mine, me.Id, To(me.Id));
        await service.AssignTemporaryResponsibleAsync(theirs, me.Id, To(other.Id));

        var result = await service.GetMyTasksAsync(me.Id);

        var task = Assert.Single(result.Data!);
        Assert.Equal("Mia", task.Title);
    }

    [Fact]
    public async Task Overview_MarksUnassignedTasks_AndCountsMatch()
    {
        await using var db = TestHelpers.CreateContext();
        var exit = await TestHelpers.AddExitAsync(db);
        var user = await TestHelpers.AddUserAsync(db);
        var service = new KnowledgeTransferService(db);
        var assignedId = await AddTaskAsync(service, exit.Id, "Asignada");
        await AddTaskAsync(service, exit.Id, "Libre 1");
        await AddTaskAsync(service, exit.Id, "Libre 2");
        await service.AssignTemporaryResponsibleAsync(assignedId, user.Id, To(user.Id));

        var result = await service.GetOverviewAsync(exit.Id);

        var overview = result.Data!;
        Assert.Equal(3, overview.TotalTasks);
        Assert.Equal(1, overview.AssignedTasks);
        Assert.Equal(2, overview.UnassignedTasks);
        Assert.Equal(overview.TotalTasks, overview.AssignedTasks + overview.UnassignedTasks);
        Assert.Equal(2, overview.Tasks.Count(t => t.AssignmentStatus == "Unassigned"));
        Assert.All(
            overview.Tasks.Where(t => t.AssignmentStatus == "Unassigned"),
            t => Assert.Null(t.TemporaryResponsibleUserId));
    }

    [Fact]
    public async Task Overview_AssignmentStatusIsIndependentOfExecutionStatus()
    {
        await using var db = TestHelpers.CreateContext();
        var exit = await TestHelpers.AddExitAsync(db);
        var service = new KnowledgeTransferService(db);
        await service.CreateTaskAsync(exit.Id, new CreateKnowledgeTransferTaskDTO
        {
            Title = "Terminada pero sin dueño",
            Description = "D",
            Status = "Completed"
        });

        var result = await service.GetOverviewAsync(exit.Id);

        var task = Assert.Single(result.Data!.Tasks);
        Assert.Equal("Completed", task.Status);
        Assert.Equal("Unassigned", task.AssignmentStatus);
    }

    [Fact]
    public async Task Overview_ReflectsReassignmentsAfterTheyHappen()
    {
        await using var db = TestHelpers.CreateContext();
        var exit = await TestHelpers.AddExitAsync(db);
        var user = await TestHelpers.AddUserAsync(db);
        var service = new KnowledgeTransferService(db);
        var taskId = await AddTaskAsync(service, exit.Id);

        var before = await service.GetOverviewAsync(exit.Id);
        await service.AssignTemporaryResponsibleAsync(taskId, user.Id, To(user.Id));
        var after = await service.GetOverviewAsync(exit.Id);

        Assert.Equal(1, before.Data!.UnassignedTasks);
        Assert.Equal(0, after.Data!.UnassignedTasks);
        Assert.Equal(1, after.Data.AssignedTasks);
    }

    [Fact]
    public async Task Overview_ShowsOnlyDataOfRequestedExit()
    {
        await using var db = TestHelpers.CreateContext();
        var exitA = await TestHelpers.AddExitAsync(db, employeeId: 1);
        var exitB = await TestHelpers.AddExitAsync(db, employeeId: 2);
        var service = new KnowledgeTransferService(db);
        await AddTaskAsync(service, exitA.Id, "De A");
        await AddTaskAsync(service, exitB.Id, "De B 1");
        await AddTaskAsync(service, exitB.Id, "De B 2");

        var result = await service.GetOverviewAsync(exitA.Id);

        Assert.Equal(1, result.Data!.TotalTasks);
        Assert.Equal("De A", result.Data.Tasks.Single().Title);
    }

    [Fact]
    public async Task Overview_DoesNotModifyStoredRecords()
    {
        await using var db = TestHelpers.CreateContext();
        var exit = await TestHelpers.AddExitAsync(db);
        var service = new KnowledgeTransferService(db);
        await AddTaskAsync(service, exit.Id);
        var before = db.KnowledgeTransferTasks.Count();

        await service.GetOverviewAsync(exit.Id);
        await service.GetOverviewAsync(exit.Id);

        Assert.Equal(before, db.KnowledgeTransferTasks.Count());
        Assert.Null(db.KnowledgeTransferTasks.Single().TemporaryResponsibleUserId);
    }

    [Fact]
    public async Task Overview_ExitNotFound_ReturnsNotFound()
    {
        await using var db = TestHelpers.CreateContext();
        var service = new KnowledgeTransferService(db);

        var result = await service.GetOverviewAsync(999);

        Assert.Equal(404, result.StatusCode);
    }
}
