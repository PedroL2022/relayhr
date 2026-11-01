using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RelayHR.Web.Models;
using RelayHR.Web.Services;

namespace RelayHR.Web.Controllers;

[ApiController]
[Route("api/checklist-tasks")]
[Authorize]
public sealed class ChecklistTasksController(IChecklistTaskService taskService) : ControllerBase
{
    /// <summary>Actualiza el estado de una tarea del checklist de salida.</summary>
    [HttpPatch("{taskId:int}/status")]
    [ProducesResponseType<ApiResponse<ChecklistTaskStatusResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<ChecklistTaskStatusResponse>>> UpdateStatus(
        int taskId,
        [FromBody] UpdateChecklistTaskStatusDTO request,
        CancellationToken cancellationToken)
    {
        // El responsable se obtiene de la sesión autenticada, no del cliente.
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized();
        }

        var result = await taskService.UpdateStatusAsync(
            taskId,
            request,
            userId,
            User.IsInRole(IdentityRoles.HumanResources),
            cancellationToken);

        return Ok(new ApiResponse<ChecklistTaskStatusResponse>(
            true,
            "Estado de la tarea actualizado correctamente.",
            result));
    }
}