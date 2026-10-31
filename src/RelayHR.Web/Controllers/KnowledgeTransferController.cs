using Microsoft.AspNetCore.Mvc;
using RelayHR.Web.DTOs.KnowledgeTransfer;
using RelayHR.Web.Services.KnowledgeTransfer;

namespace RelayHR.Web.Controllers;

[ApiController]
[Route("api/employee-exits/{employeeExitId:int}")]
public class KnowledgeTransferController : ControllerBase
{
    private readonly IKnowledgeTransferService _service;

    public KnowledgeTransferController(IKnowledgeTransferService service)
    {
        _service = service;
    }

    // POST /api/employee-exits/{employeeExitId}/projects
    [HttpPost("projects")]
    public async Task<IActionResult> CreateProject(
        int employeeExitId, [FromBody] CreateKnowledgeTransferProjectDTO dto, CancellationToken cancellationToken)
    {
        var result = await _service.CreateProjectAsync(employeeExitId, dto, cancellationToken);
        return StatusCode(result.StatusCode, result.ToResponse());
    }

    // POST /api/employee-exits/{employeeExitId}/tasks
    [HttpPost("tasks")]
    public async Task<IActionResult> CreateTask(
        int employeeExitId, [FromBody] CreateKnowledgeTransferTaskDTO dto, CancellationToken cancellationToken)
    {
        var result = await _service.CreateTaskAsync(employeeExitId, dto, cancellationToken);
        return StatusCode(result.StatusCode, result.ToResponse());
    }
}
