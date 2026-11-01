using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RelayHR.Web.Controllers;
using RelayHR.Web.Data.KnowledgeTransfer;
using RelayHR.Web.DTOs.KnowledgeTransfer;
using RelayHR.Web.Entities.Stubs;
using Xunit;

namespace RelayHR.KnowledgeTransfer.Tests;

// Levanta la aplicación real en memoria, pero reemplaza SQL Server por una base en memoria.
public class KnowledgeTransferFactory : WebApplicationFactory<KnowledgeTransferController>
{
    private readonly string _databaseName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:DefaultConnection", "Server=unused;Database=unused");

        builder.ConfigureTestServices(services =>
        {
            // Quita el registro original (SQL Server) para que no choque con el de memoria.
            var toRemove = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<KnowledgeTransferDbContext>)
                || (d.ServiceType.IsGenericType
                    && d.ServiceType.Name.StartsWith("IDbContextOptionsConfiguration")
                    && d.ServiceType.GetGenericArguments()[0] == typeof(KnowledgeTransferDbContext)))
                .ToList();
            foreach (var descriptor in toRemove)
                services.Remove(descriptor);

            services.AddDbContext<KnowledgeTransferDbContext>(o => o.UseInMemoryDatabase(_databaseName));
        });
    }
}

// Pruebas de integración: registro, asignación y consulta por HTTP.
public class KnowledgeTransferApiTests : IClassFixture<KnowledgeTransferFactory>
{
    private readonly KnowledgeTransferFactory _factory;

    public KnowledgeTransferApiTests(KnowledgeTransferFactory factory)
    {
        _factory = factory;
    }

    private async Task<(int ExitId, int UserId)> SeedAsync(bool activeUser = true)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KnowledgeTransferDbContext>();
        var exit = new EmployeeExit { EmployeeId = 1 };
        var user = new AppUser { IsActive = activeUser };
        db.EmployeeExits.Add(exit);
        db.AppUsers.Add(user);
        await db.SaveChangesAsync();
        return (exit.Id, user.Id);
    }

    private Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, object? body = null, int? userId = null)
    {
        var request = new HttpRequestMessage(method, url);
        if (body is not null)
            request.Content = JsonContent.Create(body);
        if (userId is not null)
            request.Headers.Add("X-Debug-UserId", userId.Value.ToString());
        return _factory.CreateClient().SendAsync(request);
    }

    private static async Task<T> ReadDataAsync<T>(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<T>>();
        return body!.Data!;
    }

    private async Task<int> CreateTaskAsync(int exitId, string title = "Tarea")
    {
        var response = await SendAsync(HttpMethod.Post, $"/api/employee-exits/{exitId}/tasks", new
        {
            title,
            description = "Descripcion",
            status = "Pending"
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await ReadDataAsync<KnowledgeTransferTaskResponseDTO>(response)).Id;
    }

    [Fact]
    public async Task Register_ProjectAndTasks_ReturnsCreated()
    {
        var (exitId, _) = await SeedAsync();

        var projectResponse = await SendAsync(HttpMethod.Post, $"/api/employee-exits/{exitId}/projects", new
        {
            name = "Migracion",
            description = "Pasar modulos.",
            status = "InProgress"
        });
        Assert.Equal(HttpStatusCode.Created, projectResponse.StatusCode);
        var project = await ReadDataAsync<KnowledgeTransferProjectResponseDTO>(projectResponse);

        var linked = await SendAsync(HttpMethod.Post, $"/api/employee-exits/{exitId}/tasks", new
        {
            projectId = project.Id,
            title = "Con proyecto",
            description = "D",
            status = "InProgress"
        });
        var independent = await SendAsync(HttpMethod.Post, $"/api/employee-exits/{exitId}/tasks", new
        {
            title = "Independiente",
            description = "D",
            status = "Pending"
        });

        Assert.Equal(HttpStatusCode.Created, linked.StatusCode);
        Assert.Equal(HttpStatusCode.Created, independent.StatusCode);
        Assert.Equal(project.Id, (await ReadDataAsync<KnowledgeTransferTaskResponseDTO>(linked)).ProjectId);
        Assert.Null((await ReadDataAsync<KnowledgeTransferTaskResponseDTO>(independent)).ProjectId);
    }

    [Fact]
    public async Task Register_Errors_ReturnExpectedStatusCodes()
    {
        var (exitId, _) = await SeedAsync();

        var unknownExit = await SendAsync(HttpMethod.Post, "/api/employee-exits/99999/tasks",
            new { title = "x", description = "x", status = "Pending" });
        var badStatus = await SendAsync(HttpMethod.Post, $"/api/employee-exits/{exitId}/tasks",
            new { title = "x", description = "x", status = "Hecho" });
        var badProject = await SendAsync(HttpMethod.Post, $"/api/employee-exits/{exitId}/tasks",
            new { projectId = 99999, title = "x", description = "x", status = "Pending" });

        Assert.Equal(HttpStatusCode.NotFound, unknownExit.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, badStatus.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, badProject.StatusCode);
    }

    [Fact]
    public async Task Assign_ThenQueryMyTasks_ReturnsTheAssignedTask()
    {
        var (exitId, userId) = await SeedAsync();
        var taskId = await CreateTaskAsync(exitId, "Mi tarea");

        var assign = await SendAsync(HttpMethod.Patch, $"/api/knowledge-transfer/tasks/{taskId}/assign",
            new { temporaryResponsibleUserId = userId }, userId);
        var mine = await SendAsync(HttpMethod.Get, "/api/knowledge-transfer/my-tasks", userId: userId);

        Assert.Equal(HttpStatusCode.OK, assign.StatusCode);
        Assert.Equal(HttpStatusCode.OK, mine.StatusCode);
        var tasks = await ReadDataAsync<List<MyTaskDTO>>(mine);
        Assert.Contains(tasks, t => t.Id == taskId && t.Title == "Mi tarea");
    }

    [Fact]
    public async Task Assign_WithoutIdentifiedUser_ReturnsUnauthorized()
    {
        var (exitId, userId) = await SeedAsync();
        var taskId = await CreateTaskAsync(exitId);

        var response = await SendAsync(HttpMethod.Patch, $"/api/knowledge-transfer/tasks/{taskId}/assign",
            new { temporaryResponsibleUserId = userId });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Assign_InactiveUser_ReturnsBadRequest()
    {
        var (exitId, inactiveUserId) = await SeedAsync(activeUser: false);
        var taskId = await CreateTaskAsync(exitId);

        var response = await SendAsync(HttpMethod.Patch, $"/api/knowledge-transfer/tasks/{taskId}/assign",
            new { temporaryResponsibleUserId = inactiveUserId }, inactiveUserId);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Overview_ShowsUnassignedTasks_AndUpdatesAfterAssigning()
    {
        var (exitId, userId) = await SeedAsync();
        var firstTask = await CreateTaskAsync(exitId, "Primera");
        await CreateTaskAsync(exitId, "Segunda");

        var before = await ReadDataAsync<KnowledgeTransferOverviewDTO>(
            await SendAsync(HttpMethod.Get, $"/api/employee-exits/{exitId}/knowledge-transfer"));

        await SendAsync(HttpMethod.Patch, $"/api/knowledge-transfer/tasks/{firstTask}/assign",
            new { temporaryResponsibleUserId = userId }, userId);

        var after = await ReadDataAsync<KnowledgeTransferOverviewDTO>(
            await SendAsync(HttpMethod.Get, $"/api/employee-exits/{exitId}/knowledge-transfer"));

        Assert.Equal(2, before.TotalTasks);
        Assert.Equal(2, before.UnassignedTasks);
        Assert.All(before.Tasks, t => Assert.Equal("Unassigned", t.AssignmentStatus));

        Assert.Equal(2, after.TotalTasks);
        Assert.Equal(1, after.AssignedTasks);
        Assert.Equal(1, after.UnassignedTasks);
        Assert.Equal("Assigned", after.Tasks.Single(t => t.Id == firstTask).AssignmentStatus);
    }

    [Fact]
    public async Task Overview_UnknownExit_ReturnsNotFound()
    {
        var response = await SendAsync(HttpMethod.Get, "/api/employee-exits/99999/knowledge-transfer");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
