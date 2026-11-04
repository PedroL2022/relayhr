using RelayHR.Web.DTOs.KnowledgeTransfer;
using RelayHR.Web.Services.KnowledgeTransfer;
using Xunit;

namespace RelayHR.KnowledgeTransfer.Tests;

// Pruebas unitarias: registro de proyectos y tareas (SCRUM-25).
public class KnowledgeTransferRegistrationTests
{
    private static CreateKnowledgeTransferTaskDTO ValidTask(int? projectId = null) => new()
    {
        ProjectId = projectId,
        Title = "Actualizar documentacion",
        Description = "Completar los modulos pendientes.",
        Status = "InProgress"
    };

    [Fact]
    public async Task CreateProject_WithValidData_ReturnsCreated()
    {
        await using var db = TestHelpers.CreateContext();
        var exit = await TestHelpers.AddExitAsync(db, employeeId: 7);
        var service = new KnowledgeTransferService(db);

        var result = await service.CreateProjectAsync(exit.Id, new CreateKnowledgeTransferProjectDTO
        {
            Name = "Migracion",
            Description = "Pasar modulos al nuevo servidor."
        });

        Assert.Equal(201, result.StatusCode);
        Assert.Equal("Pending", result.Data!.Status); // sin estado, queda Pending
        Assert.Equal(7, result.Data.DepartingEmployeeId);
    }

    [Fact]
    public async Task CreateProject_ExitNotFound_ReturnsNotFound()
    {
        await using var db = TestHelpers.CreateContext();
        var service = new KnowledgeTransferService(db);

        var result = await service.CreateProjectAsync(999, new CreateKnowledgeTransferProjectDTO
        {
            Name = "x",
            Description = "x"
        });

        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task CreateTask_WithoutProject_IsAcceptedAsIndependent()
    {
        await using var db = TestHelpers.CreateContext();
        var exit = await TestHelpers.AddExitAsync(db);
        var service = new KnowledgeTransferService(db);

        var result = await service.CreateTaskAsync(exit.Id, ValidTask());

        Assert.Equal(201, result.StatusCode);
        Assert.Null(result.Data!.ProjectId);
    }

    [Fact]
    public async Task CreateTask_WithProjectOfSameExit_IsLinked()
    {
        await using var db = TestHelpers.CreateContext();
        var exit = await TestHelpers.AddExitAsync(db);
        var service = new KnowledgeTransferService(db);
        var project = await service.CreateProjectAsync(exit.Id, new CreateKnowledgeTransferProjectDTO
        {
            Name = "P",
            Description = "D"
        });

        var result = await service.CreateTaskAsync(exit.Id, ValidTask(project.Data!.Id));

        Assert.Equal(201, result.StatusCode);
        Assert.Equal(project.Data.Id, result.Data!.ProjectId);
    }

    [Fact]
    public async Task CreateTask_ExitNotFound_ReturnsNotFound()
    {
        await using var db = TestHelpers.CreateContext();
        var service = new KnowledgeTransferService(db);

        var result = await service.CreateTaskAsync(999, ValidTask());

        Assert.Equal(404, result.StatusCode);
    }

    [Theory]
    [InlineData("Hecho")]
    [InlineData("7")]
    [InlineData("")]
    public async Task CreateTask_InvalidStatus_ReturnsBadRequest(string status)
    {
        await using var db = TestHelpers.CreateContext();
        var exit = await TestHelpers.AddExitAsync(db);
        var service = new KnowledgeTransferService(db);
        var dto = ValidTask();
        dto.Status = status;

        var result = await service.CreateTaskAsync(exit.Id, dto);

        Assert.Equal(400, result.StatusCode);
    }

    [Theory]
    [InlineData(null, "descripcion")]
    [InlineData("titulo", null)]
    [InlineData("   ", "descripcion")]
    public async Task CreateTask_MissingTitleOrDescription_ReturnsBadRequest(string? title, string? description)
    {
        await using var db = TestHelpers.CreateContext();
        var exit = await TestHelpers.AddExitAsync(db);
        var service = new KnowledgeTransferService(db);
        var dto = ValidTask();
        dto.Title = title;
        dto.Description = description;

        var result = await service.CreateTaskAsync(exit.Id, dto);

        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task CreateTask_ProjectFromAnotherExit_ReturnsBadRequest()
    {
        await using var db = TestHelpers.CreateContext();
        var exitA = await TestHelpers.AddExitAsync(db, employeeId: 1);
        var exitB = await TestHelpers.AddExitAsync(db, employeeId: 2);
        var service = new KnowledgeTransferService(db);
        var projectOfA = await service.CreateProjectAsync(exitA.Id, new CreateKnowledgeTransferProjectDTO
        {
            Name = "P",
            Description = "D"
        });

        var result = await service.CreateTaskAsync(exitB.Id, ValidTask(projectOfA.Data!.Id));

        Assert.Equal(400, result.StatusCode);
    }
}
