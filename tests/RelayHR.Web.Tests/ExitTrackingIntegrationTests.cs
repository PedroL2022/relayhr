using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RelayHR.Web.Entities;
using Xunit;

namespace RelayHR.Web.Tests.Integration;

public class ExitTrackingIntegrationTests
{
    private static async Task<int> CreateExitAsync(TestApplication app)
    {
        var response = await app.Client.PostAsJsonAsync("/api/employee-exits", new
        {
            employeeId = 15,
            effectiveDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(10)
        });
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("data").GetProperty("employeeExitId").GetInt32();
    }

    private static Task<List<ChecklistTask>> GetTasksAsync(TestApplication app, int exitId) =>
        app.InScopeAsync(db => db.ChecklistTasks
            .AsNoTracking()
            .Where(t => t.ExitChecklist.EmployeeExitId == exitId)
            .ToListAsync());

    private static Task<HttpResponseMessage> PatchAsync(
        HttpClient client, int taskId, string status, string? notes = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/checklist-tasks/{taskId}/status")
        {
            Content = JsonContent.Create(new { status, notes })
        };
        return client.SendAsync(request);
    }

    private static async Task CompleteRequiredTasksAsync(TestApplication app, int exitId)
    {
        var tasks = await GetTasksAsync(app, exitId);
        foreach (var task in tasks.Where(t => t.IsRequired))
        {
            using var response = await PatchAsync(app.Client, task.Id, "Completed");
            response.EnsureSuccessStatusCode();
        }
    }

    [Fact]
    public async Task Patch_completa_la_tarea_y_guarda_fecha_y_usuario_autenticado()
    {
        await using var app = await TestApplication.StartAsync();
        var exitId = await CreateExitAsync(app);
        var task = (await GetTasksAsync(app, exitId)).First(t => t.IsRequired);

        using var response = await PatchAsync(
            app.Client, task.Id, "Completed", "Equipo devuelto y verificado.");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = (await GetTasksAsync(app, exitId)).Single(t => t.Id == task.Id);
        Assert.Equal(ChecklistTaskStatus.Completed, saved.Status);
        Assert.NotNull(saved.CompletedAt);
        Assert.Equal(104, saved.CompletedByUserId);
        Assert.Equal("Equipo devuelto y verificado.", saved.Notes);
        Assert.Equal(task.IsRequired, saved.IsRequired);
    }

    [Fact]
    public async Task Confirmar_revocacion_de_accesos_registra_al_responsable()
    {
        await using var app = await TestApplication.StartAsync();
        var exitId = await CreateExitAsync(app);
        var access = (await GetTasksAsync(app, exitId))
            .Single(t => t.Title == "Desactivar accesos y cuentas corporativas");

        using var response = await PatchAsync(
            app.Client, access.Id, "Completed", "Accesos revocados.");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = (await GetTasksAsync(app, exitId)).Single(t => t.Id == access.Id);
        Assert.Equal(ChecklistTaskStatus.Completed, saved.Status);
        Assert.Equal(104, saved.CompletedByUserId);
        Assert.NotNull(saved.CompletedAt);
    }

    [Fact]
    public async Task Patch_sin_sesion_devuelve_401()
    {
        await using var app = await TestApplication.StartAsync();
        var exitId = await CreateExitAsync(app);
        var task = (await GetTasksAsync(app, exitId)).First();

        using var anonymous = app.CreateUnauthenticatedClient();
        using var response = await PatchAsync(anonymous, task.Id, "Completed");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Patch_de_usuario_sin_permiso_devuelve_403()
    {
        await using var app = await TestApplication.StartAsync();
        var exitId = await CreateExitAsync(app);
        var task = (await GetTasksAsync(app, exitId)).First();

        await app.LoginAsync("employee@example.test");
        using var response = await PatchAsync(app.Client, task.Id, "Completed");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Cierre_se_bloquea_y_lista_las_tareas_obligatorias_pendientes()
    {
        await using var app = await TestApplication.StartAsync();
        var exitId = await CreateExitAsync(app);
        var required = (await GetTasksAsync(app, exitId)).Count(t => t.IsRequired);

        using var response = await app.Client.PostAsync(
            $"/api/employee-exits/{exitId}/close", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(json.GetProperty("success").GetBoolean());
        Assert.Equal(
            required,
            json.GetProperty("data").GetProperty("pendingTasks").GetArrayLength());
    }

    [Fact]
    public async Task Cierre_exitoso_registra_fecha_y_usuario_y_las_opcionales_no_bloquean()
    {
        await using var app = await TestApplication.StartAsync();
        var exitId = await CreateExitAsync(app);
        await CompleteRequiredTasksAsync(app, exitId);

        // Las tareas opcionales siguen pendientes y no deben impedir el cierre.
        var optionalPending = (await GetTasksAsync(app, exitId))
            .Count(t => !t.IsRequired && t.Status != ChecklistTaskStatus.Completed);
        Assert.True(optionalPending > 0);

        using var response = await app.Client.PostAsync(
            $"/api/employee-exits/{exitId}/close", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var exit = await app.InScopeAsync(db =>
            db.EmployeeExits.AsNoTracking().SingleAsync(e => e.Id == exitId));
        Assert.Equal(EmployeeExitStatuses.Closed, exit.Status);
        Assert.NotNull(exit.ClosedAt);
        Assert.Equal(104, exit.ClosedByUserId);
    }

    [Fact]
    public async Task No_se_puede_cerrar_dos_veces()
    {
        await using var app = await TestApplication.StartAsync();
        var exitId = await CreateExitAsync(app);
        await CompleteRequiredTasksAsync(app, exitId);

        using var first = await app.Client.PostAsync($"/api/employee-exits/{exitId}/close", null);
        using var second = await app.Client.PostAsync($"/api/employee-exits/{exitId}/close", null);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task No_se_actualizan_tareas_de_un_proceso_cerrado()
    {
        await using var app = await TestApplication.StartAsync();
        var exitId = await CreateExitAsync(app);
        await CompleteRequiredTasksAsync(app, exitId);
        using var close = await app.Client.PostAsync($"/api/employee-exits/{exitId}/close", null);
        close.EnsureSuccessStatusCode();

        var task = (await GetTasksAsync(app, exitId)).First();
        using var response = await PatchAsync(app.Client, task.Id, "Pending");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Progreso_calcula_conteos_y_porcentaje()
    {
        await using var app = await TestApplication.StartAsync();
        var exitId = await CreateExitAsync(app);
        var task = (await GetTasksAsync(app, exitId)).First(t => t.IsRequired);
        using var patch = await PatchAsync(app.Client, task.Id, "Completed");
        patch.EnsureSuccessStatusCode();

        using var response = await app.Client.GetAsync($"/api/employee-exits/{exitId}/progress");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
        Assert.Equal(6, data.GetProperty("totalTasks").GetInt32());
        Assert.Equal(1, data.GetProperty("completedTasks").GetInt32());
        Assert.Equal(5, data.GetProperty("pendingTasks").GetInt32());
        Assert.Equal(0, data.GetProperty("inProgressTasks").GetInt32());
        Assert.Equal(16.67, data.GetProperty("progressPercentage").GetDouble());
        Assert.Equal(4, data.GetProperty("requiredIncompleteTasks").GetInt32());
        Assert.False(data.GetProperty("canClose").GetBoolean());
    }
}