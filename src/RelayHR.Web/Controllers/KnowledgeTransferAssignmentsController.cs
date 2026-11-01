using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using RelayHR.Web.DTOs.KnowledgeTransfer;
using RelayHR.Web.Services.KnowledgeTransfer;

namespace RelayHR.Web.Controllers;

[ApiController]
[Route("api/knowledge-transfer")]
public class KnowledgeTransferAssignmentsController : ControllerBase
{
    private readonly IKnowledgeTransferService _service;
    private readonly IWebHostEnvironment _environment;

    public KnowledgeTransferAssignmentsController(IKnowledgeTransferService service, IWebHostEnvironment environment)
    {
        _service = service;
        _environment = environment;
    }

    // PATCH /api/knowledge-transfer/tasks/{taskId}/assign
    [HttpPatch("tasks/{taskId:int}/assign")]
    public async Task<IActionResult> Assign(
        int taskId, [FromBody] AssignTemporaryResponsibleDTO dto, CancellationToken cancellationToken)
    {
        var result = await _service.AssignTemporaryResponsibleAsync(taskId, GetCurrentUserId(), dto, cancellationToken);
        return StatusCode(result.StatusCode, result.ToResponse());
    }

    // GET /api/knowledge-transfer/my-tasks  (el usuario sale de la autenticación, nunca de un parámetro)
    [HttpGet("my-tasks")]
    public async Task<IActionResult> MyTasks(CancellationToken cancellationToken)
    {
        var result = await _service.GetMyTasksAsync(GetCurrentUserId(), cancellationToken);
        return StatusCode(result.StatusCode, result.ToResponse());
    }

    private int? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // TEMPORAL (solo Development): mientras no exista el login de la HU-08, se permite simular
        // al usuario con el encabezado X-Debug-UserId. Borrar este bloque al unir con la HU-08.
        if (value is null
            && _environment.IsDevelopment()
            && Request.Headers.TryGetValue("X-Debug-UserId", out var header))
        {
            value = header.ToString();
        }

        return int.TryParse(value, out var id) ? id : null;
    }
}
