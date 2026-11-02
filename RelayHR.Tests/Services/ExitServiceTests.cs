using Microsoft.EntityFrameworkCore;
using RelayHR.Tests.Support;
using RelayHR.Web.Data;
using RelayHR.Web.Entities;
using RelayHR.Web.Services;

namespace RelayHR.Tests.Services;

/// <summary>Registro de salida (HU-01 y HU-04), cierre (HU-02) y traspaso (HU-03).</summary>
public class ExitServiceTests
{
    private static readonly DateOnly ExitDate = new(2026, 11, 20);

    private static (AppDbContext Db, ExitService Service, TestClock Clock) Setup()
    {
        var db = TestDb.Create();
        var clock = new TestClock();
        return (db, Build.Exit(db, clock), clock);
    }

    // ---------- HU-01 / HU-04: registrar la salida ----------

    [Fact]
    public async Task Register_CreatesExitChecklistAndVacancyInOneStep()
    {
        var (db, service, clock) = Setup();
        var employee = await TestDb.AddEmployeeAsync(db);

        var result = await service.RegisterAsync(employee.EmployeeId, ExitDate, "Renuncia", "Admin");

        Assert.True(result.Succeeded);

        var exit = await db.EmployeeExits.Include(e => e.Tasks).Include(e => e.ExpressVacancy)
            .FirstAsync(e => e.EmployeeExitId == result.Value);

        Assert.Equal(ExitStatus.InProgress, exit.Status);
        Assert.Equal("Admin", exit.RegisteredBy);
        Assert.Equal(clock.Now.UtcDateTime, exit.CreatedAt);
        Assert.Equal(ExitChecklistTemplate.Items.Count, exit.Tasks.Count);
        Assert.NotNull(exit.ExpressVacancy);
        Assert.Equal(VacancyStatus.PendingReview, exit.ExpressVacancy!.Status);
        Assert.Equal("Desarrollador Backend", exit.ExpressVacancy.JobTitle);
    }

    [Fact]
    public async Task Register_MarksTheEmployeeAsInactive()
    {
        var (db, service, _) = Setup();
        var employee = await TestDb.AddEmployeeAsync(db);

        await service.RegisterAsync(employee.EmployeeId, ExitDate, null, "Admin");

        Assert.False((await db.Employees.FirstAsync(e => e.EmployeeId == employee.EmployeeId)).IsActive);
    }

    [Fact]
    public async Task Register_TheSameEmployeeTwice_IsRejected()
    {
        var (db, service, _) = Setup();
        var employee = await TestDb.AddEmployeeAsync(db);
        await service.RegisterAsync(employee.EmployeeId, ExitDate, null, "Admin");

        var second = await service.RegisterAsync(employee.EmployeeId, ExitDate, null, "Admin");

        Assert.False(second.Succeeded);
        Assert.Equal(1, await db.EmployeeExits.CountAsync());
        Assert.Equal(1, await db.ExpressVacancies.CountAsync());
    }

    [Fact]
    public async Task Register_WithUnknownEmployee_Fails()
    {
        var (_, service, _) = Setup();

        var result = await service.RegisterAsync(999, ExitDate, null, "Admin");

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Register_EmployeeWithIncompletePosition_GeneratesVacancyWithMissingFields()
    {
        var (db, service, _) = Setup();
        var employee = await TestDb.AddEmployeeAsync(db, completePosition: false);

        await service.RegisterAsync(employee.EmployeeId, ExitDate, null, "Admin");

        var vacancy = await db.ExpressVacancies.FirstAsync();
        Assert.Equal(new[] { "Descripción", "Requisitos" }, vacancy.GetMissingFields());
    }

    // ---------- HU-02: control de cierre ----------

    [Fact]
    public async Task Close_WithPendingMandatoryTasks_IsBlockedAndTheMessageListsThem()
    {
        var (db, service, _) = Setup();
        var employee = await TestDb.AddEmployeeAsync(db);
        var exitId = (await service.RegisterAsync(employee.EmployeeId, ExitDate, null, "Admin")).Value;

        var result = await service.CloseAsync(exitId, "Admin");

        Assert.False(result.Succeeded);
        Assert.Contains("Revocar accesos", result.Error!);
        Assert.Contains("Devolver laptop", result.Error!);
        Assert.Equal(ExitStatus.InProgress, (await db.EmployeeExits.FirstAsync()).Status);
    }

    [Fact]
    public async Task Close_AfterCompletingMandatoryTasks_ClosesAndRecordsUser()
    {
        var (db, service, clock) = Setup();
        var employee = await TestDb.AddEmployeeAsync(db);
        var exitId = (await service.RegisterAsync(employee.EmployeeId, ExitDate, null, "Admin")).Value;

        var mandatory = await db.ExitTasks.Where(t => t.IsMandatory).ToListAsync();
        foreach (var task in mandatory)
        {
            Assert.True((await service.SetTaskStatusAsync(task.ExitTaskId, ExitTaskStatus.Completed, "Equipo")).Succeeded);
        }

        var result = await service.CloseAsync(exitId, "Admin RRHH");

        Assert.True(result.Succeeded);
        var exit = await db.EmployeeExits.FirstAsync();
        Assert.Equal(ExitStatus.Closed, exit.Status);
        Assert.Equal("Admin RRHH", exit.ClosedBy);
        Assert.Equal(clock.Now.UtcDateTime, exit.ClosedAt);
    }

    [Fact]
    public async Task SetTaskStatus_Completed_RecordsDateAndResponsible()
    {
        var (db, service, clock) = Setup();
        var employee = await TestDb.AddEmployeeAsync(db);
        await service.RegisterAsync(employee.EmployeeId, ExitDate, null, "Admin");
        var access = await db.ExitTasks.FirstAsync(t => t.Kind == ExitTaskKind.AccessRevocation);

        var result = await service.SetTaskStatusAsync(access.ExitTaskId, ExitTaskStatus.Completed, "María (Tecnología)");

        Assert.True(result.Succeeded);
        var saved = await db.ExitTasks.FirstAsync(t => t.ExitTaskId == access.ExitTaskId);
        Assert.Equal(ExitTaskStatus.Completed, saved.Status);
        Assert.Equal("María (Tecnología)", saved.CompletedBy);
        Assert.Equal(clock.Now.UtcDateTime, saved.CompletedAt);
    }

    [Fact]
    public async Task AClosedExit_CannotBeModifiedAnymore()
    {
        var (db, service, _) = Setup();
        var employee = await TestDb.AddEmployeeAsync(db);
        var exitId = (await service.RegisterAsync(employee.EmployeeId, ExitDate, null, "Admin")).Value;
        foreach (var task in await db.ExitTasks.Where(t => t.IsMandatory).ToListAsync())
        {
            await service.SetTaskStatusAsync(task.ExitTaskId, ExitTaskStatus.Completed, "x");
        }
        await service.CloseAsync(exitId, "Admin");
        var anyTask = await db.ExitTasks.FirstAsync();

        Assert.False((await service.SetTaskStatusAsync(anyTask.ExitTaskId, ExitTaskStatus.Pending, "x")).Succeeded);
        Assert.False((await service.UpdateTaskAsync(anyTask.ExitTaskId, "Otro", ExitDate)).Succeeded);
        Assert.False((await service.CloseAsync(exitId, "Admin")).Succeeded);
        Assert.False((await service.AddKnowledgeItemAsync(exitId, "t", "d", "s", null)).Succeeded);
    }

    [Fact]
    public async Task UpdateTask_ChangesResponsibleAndDueDate_ButRequiresAResponsible()
    {
        var (db, service, _) = Setup();
        var employee = await TestDb.AddEmployeeAsync(db);
        await service.RegisterAsync(employee.EmployeeId, ExitDate, null, "Admin");
        var task = await db.ExitTasks.FirstAsync();

        Assert.True((await service.UpdateTaskAsync(task.ExitTaskId, "  Carlos Gómez ", ExitDate.AddDays(7))).Succeeded);
        Assert.False((await service.UpdateTaskAsync(task.ExitTaskId, "   ", ExitDate)).Succeeded);

        var saved = await db.ExitTasks.FirstAsync(t => t.ExitTaskId == task.ExitTaskId);
        Assert.Equal("Carlos Gómez", saved.ResponsibleName);
        Assert.Equal(ExitDate.AddDays(7), saved.DueDate);
    }

    // ---------- HU-03: traspaso de conocimiento ----------

    [Fact]
    public async Task AddKnowledgeItem_SavesDescriptionAndCurrentStatus()
    {
        var (db, service, _) = Setup();
        var leaving = await TestDb.AddEmployeeAsync(db);
        var exitId = (await service.RegisterAsync(leaving.EmployeeId, ExitDate, null, "Admin")).Value;

        var result = await service.AddKnowledgeItemAsync(exitId, "  Migración de la API ", "Pasar los endpoints a la v2", "60 % avanzado", null);

        Assert.True(result.Succeeded);
        var item = await db.KnowledgeItems.FirstAsync();
        Assert.Equal("Migración de la API", item.Title);
        Assert.Equal("Pasar los endpoints a la v2", item.Description);
        Assert.Equal("60 % avanzado", item.CurrentStatus);
        Assert.Equal(exitId, item.EmployeeExitId);
    }

    [Fact]
    public async Task KnowledgeItem_WithoutAssignee_IsMarkedAsUnassigned_AndAssigningItShowsInTheAssigneesList()
    {
        var (db, service, _) = Setup();
        var leaving = await TestDb.AddEmployeeAsync(db, "Ana");
        var colleague = await TestDb.AddEmployeeAsync(db, "Luis");
        var exitId = (await service.RegisterAsync(leaving.EmployeeId, ExitDate, null, "Admin")).Value;
        var itemId = (await service.AddKnowledgeItemAsync(exitId, "Informe mensual", "Preparar el informe", "Pendiente", null)).Value;

        Assert.True((await db.KnowledgeItems.FirstAsync()).IsUnassigned);

        var result = await service.AssignKnowledgeItemAsync(itemId, colleague.EmployeeId);

        Assert.True(result.Succeeded);
        var assigned = await db.KnowledgeItems.Where(k => k.AssignedToEmployeeId == colleague.EmployeeId).ToListAsync();
        Assert.Single(assigned);
        Assert.False(assigned[0].IsUnassigned);
    }

    [Fact]
    public async Task AssignKnowledgeItem_CanBeRemovedWithNull()
    {
        var (db, service, _) = Setup();
        var leaving = await TestDb.AddEmployeeAsync(db, "Ana");
        var colleague = await TestDb.AddEmployeeAsync(db, "Luis");
        var exitId = (await service.RegisterAsync(leaving.EmployeeId, ExitDate, null, "Admin")).Value;
        var itemId = (await service.AddKnowledgeItemAsync(exitId, "t", "d", "s", colleague.EmployeeId)).Value;

        await service.AssignKnowledgeItemAsync(itemId, null);

        Assert.True((await db.KnowledgeItems.FirstAsync()).IsUnassigned);
    }

    [Fact]
    public async Task TheAssignee_MustBeAnActiveEmployee_DifferentFromTheOneLeaving()
    {
        var (db, service, _) = Setup();
        var leaving = await TestDb.AddEmployeeAsync(db, "Ana");
        var alsoLeaving = await TestDb.AddEmployeeAsync(db, "Pedro");
        var exitId = (await service.RegisterAsync(leaving.EmployeeId, ExitDate, null, "Admin")).Value;
        await service.RegisterAsync(alsoLeaving.EmployeeId, ExitDate, null, "Admin");
        var itemId = (await service.AddKnowledgeItemAsync(exitId, "t", "d", "s", null)).Value;

        Assert.False((await service.AssignKnowledgeItemAsync(itemId, leaving.EmployeeId)).Succeeded);
        Assert.False((await service.AssignKnowledgeItemAsync(itemId, alsoLeaving.EmployeeId)).Succeeded);
        Assert.False((await service.AssignKnowledgeItemAsync(itemId, 12345)).Succeeded);
        Assert.False((await service.AddKnowledgeItemAsync(exitId, "t", "d", "s", leaving.EmployeeId)).Succeeded);
    }
}
