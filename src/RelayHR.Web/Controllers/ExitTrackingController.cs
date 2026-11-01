using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RelayHR.Web.Models;
using RelayHR.Web.Services;

namespace RelayHR.Web.Controllers;

[ApiController]
[Route("api/employee-exits")]
[Authorize(Roles = IdentityRoles.HumanResources)]
public sealed class ExitTrackingController(IExitTrackingService service) : ControllerBase
{
    /// <summary>Cierra el proceso de salida si todas las tareas obligatorias están completadas.</summary>
    [HttpPost("{employeeExitId:int}/close")]
    public async Task<IActionResult> Close(int employeeExitId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized();
        }

        var result = await service.CloseAsync(employeeExitId, userId, cancellationToken);

        if (result.Closed is null)
        {
            return StatusCode(StatusCodes.Status409Conflict,
                new ApiResponse<CloseBlockedData>(
                    false,
                    result.BlockedMessage!,
                    new CloseBlockedData(result.PendingTasks!)));
        }

        return Ok(new ApiResponse<CloseExitResponse>(
            true, "Proceso cerrado correctamente.", result.Closed));
    }

    /// <summary>Consulta el avance del checklist. No modifica datos.</summary>
    [HttpGet("{employeeExitId:int}/progress")]
    public async Task<ActionResult<ApiResponse<ExitProgressResponse>>> GetProgress(
        int employeeExitId,
        CancellationToken cancellationToken)
    {
        var progress = await service.GetProgressAsync(employeeExitId, cancellationToken);
        return Ok(new ApiResponse<ExitProgressResponse>(
            true, "Avance del checklist obtenido correctamente.", progress));
    }
}