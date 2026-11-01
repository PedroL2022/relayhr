using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RelayHR.Web.Data;
using RelayHR.Web.Entities;
using RelayHR.Web.Models;
using RelayHR.Web.Services;
using Xunit.Abstractions;

namespace RelayHR.Web.Tests.Integration;

public sealed class EmployeeExitApiTests(ITestOutputHelper output)
{
    [Fact]
    public async Task Registering_an_exit_makes_its_complete_checklist_available_within_ten_seconds()
    {
        await using var application = await TestApplication.StartAsync();
        var stopwatch = Stopwatch.StartNew();

        using var createResponse = await application.Client.PostAsJsonAsync(
            "/api/employee-exits",
            ValidRequest());
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content
            .ReadFromJsonAsync<ApiResponse<CreateEmployeeExitResponse>>();
        Assert.NotNull(created);

        using var checklistResponse = await application.Client.GetAsync(
            $"/api/employee-exits/{created.Data.EmployeeExitId}/checklist");
        Assert.Equal(HttpStatusCode.OK, checklistResponse.StatusCode);
        stopwatch.Stop();

        var checklist = await checklistResponse.Content
            .ReadFromJsonAsync<ApiResponse<ExitChecklistResponseDTO>>();
        Assert.NotNull(checklist);
        Assert.Equal(created.Data.ChecklistId, checklist.Data.ChecklistId);
        Assert.Equal(created.Data.EmployeeExitId, checklist.Data.EmployeeExitId);
        Assert.Equal(6, checklist.Data.Tasks.Count);
        Assert.Equal(
            new[] { "Finanzas", "Recursos Humanos", "Tecnología" },
            checklist.Data.Tasks.Select(task => task.Department).Distinct().ToArray());
        Assert.All(checklist.Data.Tasks, task =>
        {
            Assert.True(task.ResponsibleUserId > 0);
            Assert.False(string.IsNullOrWhiteSpace(task.ResponsibleName));
            Assert.True(task.DueDate >= DateOnly.FromDateTime(DateTime.UtcNow));
            Assert.Equal(nameof(ChecklistTaskStatus.Pending), task.Status);
        });

        var storedCounts = await application.InScopeAsync(async dbContext => new
        {
            Exits = await dbContext.EmployeeExits.CountAsync(),
            Checklists = await dbContext.ExitChecklists.CountAsync(),
            Tasks = await dbContext.ChecklistTasks.CountAsync()
        });
        Assert.Equal(1, storedCounts.Exits);
        Assert.Equal(1, storedCounts.Checklists);
        Assert.Equal(6, storedCounts.Tasks);
        Assert.True(
            stopwatch.Elapsed < TimeSpan.FromSeconds(10),
            $"Checklist tardó {stopwatch.Elapsed.TotalMilliseconds:N0} ms en estar disponible.");
        output.WriteLine(
            "Registro y consulta del checklist disponibles en {0:N0} ms.",
            stopwatch.Elapsed.TotalMilliseconds);
    }

    [Fact]
    public async Task Registering_an_unknown_employee_returns_not_found()
    {
        await using var application = await TestApplication.StartAsync();

        using var response = await application.Client.PostAsJsonAsync(
            "/api/employee-exits",
            new
            {
                employeeId = 9999,
                effectiveDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(10)
            });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Registering_the_same_employee_twice_returns_conflict_without_duplicate_records()
    {
        await using var application = await TestApplication.StartAsync();
        using var firstResponse = await application.Client.PostAsJsonAsync(
            "/api/employee-exits",
            ValidRequest());
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        using var secondResponse = await application.Client.PostAsJsonAsync(
            "/api/employee-exits",
            ValidRequest());

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
        var counts = await application.InScopeAsync(async dbContext => new
        {
            Exits = await dbContext.EmployeeExits.CountAsync(),
            Checklists = await dbContext.ExitChecklists.CountAsync(),
            Tasks = await dbContext.ChecklistTasks.CountAsync()
        });
        Assert.Equal(1, counts.Exits);
        Assert.Equal(1, counts.Checklists);
        Assert.Equal(6, counts.Tasks);
    }

    [Fact]
    public async Task A_save_failure_while_inserting_checklist_tasks_rolls_back_the_exit_and_checklist()
    {
        await using var application = await TestApplication.StartAsync();
        await application.InScopeAsync(async dbContext =>
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                """
                CREATE TRIGGER FailChecklistTaskInsert
                BEFORE INSERT ON ChecklistTasks
                BEGIN
                    SELECT RAISE(ABORT, 'Simulated checklist persistence failure');
                END;
                """);
            return true;
        });

        using var response = await application.Client.PostAsJsonAsync(
            "/api/employee-exits",
            ValidRequest());

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var counts = await application.InScopeAsync(async dbContext => new
        {
            Exits = await dbContext.EmployeeExits.CountAsync(),
            Checklists = await dbContext.ExitChecklists.CountAsync(),
            Tasks = await dbContext.ChecklistTasks.CountAsync()
        });
        Assert.Equal(0, counts.Exits);
        Assert.Equal(0, counts.Checklists);
        Assert.Equal(0, counts.Tasks);
    }

    [Fact]
    public async Task A_checklist_generation_failure_does_not_persist_an_exit()
    {
        await using var application = await TestApplication.StartAsync(failChecklistGeneration: true);

        using var response = await application.Client.PostAsJsonAsync(
            "/api/employee-exits",
            ValidRequest());

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var counts = await application.InScopeAsync(async dbContext => new
        {
            Exits = await dbContext.EmployeeExits.CountAsync(),
            Checklists = await dbContext.ExitChecklists.CountAsync()
        });
        Assert.Equal(0, counts.Exits);
        Assert.Equal(0, counts.Checklists);
    }

    [Fact]
    public async Task Querying_a_missing_exit_returns_not_found()
    {
        await using var application = await TestApplication.StartAsync();

        using var response = await application.Client.GetAsync(
            "/api/employee-exits/9999/checklist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Exit_endpoints_require_authentication_and_the_hr_role()
    {
        await using var application = await TestApplication.StartAsync();
        using var anonymousClient = application.CreateUnauthenticatedClient();
        using var anonymousResponse = await anonymousClient.PostAsJsonAsync(
            "/api/employee-exits",
            ValidRequest());
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);

        using var nonHrClient = application.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        using var loginResponse = await nonHrClient.PostAsJsonAsync(
            "/api/auth/login",
            new { email = "employee@example.test", password = "RelayHr-Test-123!" });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        using var forbiddenResponse = await nonHrClient.PostAsJsonAsync(
            "/api/employee-exits",
            ValidRequest());
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);
    }

    [Fact]
    public async Task Checklist_query_is_read_only_and_returns_tasks_sorted_by_department_then_due_date()
    {
        await using var application = await TestApplication.StartAsync();
        using var createResponse = await application.Client.PostAsJsonAsync(
            "/api/employee-exits",
            ValidRequest());
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content
            .ReadFromJsonAsync<ApiResponse<CreateEmployeeExitResponse>>();
        Assert.NotNull(created);

        var before = await application.InScopeAsync(async dbContext => new
        {
            Exits = await dbContext.EmployeeExits.CountAsync(),
            Checklists = await dbContext.ExitChecklists.CountAsync(),
            Tasks = await dbContext.ChecklistTasks.CountAsync()
        });

        using var response = await application.Client.GetAsync(
            $"/api/employee-exits/{created.Data.EmployeeExitId}/checklist");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content
            .ReadFromJsonAsync<ApiResponse<ExitChecklistResponseDTO>>();
        Assert.NotNull(payload);

        var orderedTasks = payload.Data.Tasks.ToArray();
        Assert.Equal(
            orderedTasks.OrderBy(task => task.Department, StringComparer.OrdinalIgnoreCase)
                .ThenBy(task => task.DueDate)
                .ThenBy(task => task.Id),
            orderedTasks);

        var after = await application.InScopeAsync(async dbContext => new
        {
            Exits = await dbContext.EmployeeExits.CountAsync(),
            Checklists = await dbContext.ExitChecklists.CountAsync(),
            Tasks = await dbContext.ChecklistTasks.CountAsync()
        });
        Assert.Equal(before, after);
    }

    private static object ValidRequest() => new
    {
        employeeId = 15,
        effectiveDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(14)
    };
}
