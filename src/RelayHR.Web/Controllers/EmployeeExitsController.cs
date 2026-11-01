using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RelayHR.Web.Models;
using RelayHR.Web.Services;

namespace RelayHR.Web.Controllers;

[ApiController]
[Route("api/employee-exits")]
[Authorize(Roles = IdentityRoles.HumanResources)]
public sealed class EmployeeExitsController(IEmployeeExitService employeeExitService) : ControllerBase
{
    /// <summary>Registra la salida de un empleado y genera su checklist.</summary>
    [HttpPost]
    [ProducesResponseType<ApiResponse<CreateEmployeeExitResponse>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<CreateEmployeeExitResponse>>> Create(
        [FromBody] CreateEmployeeExitDTO request,
        CancellationToken cancellationToken)
    {
        var result = await employeeExitService.CreateAsync(request, cancellationToken);
        var response = new ApiResponse<CreateEmployeeExitResponse>(
            true,
            "Salida registrada correctamente.",
            result);

        return CreatedAtAction(
            nameof(GetChecklist),
            new { employeeExitId = result.EmployeeExitId },
            response);
    }

    /// <summary>Consulta las tareas del checklist asociado a una salida.</summary>
    [HttpGet("{employeeExitId:int}/checklist")]
    [ProducesResponseType<ApiResponse<ExitChecklistResponseDTO>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<ExitChecklistResponseDTO>>> GetChecklist(
        int employeeExitId,
        CancellationToken cancellationToken)
    {
        var checklist = await employeeExitService.GetChecklistAsync(
            employeeExitId,
            cancellationToken);

        if (checklist is null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Checklist no encontrado",
                Detail = "No existe una salida con ese identificador.",
                Instance = HttpContext.Request.Path
            });
        }

        return Ok(new ApiResponse<ExitChecklistResponseDTO>(
            true,
            "Checklist obtenido correctamente.",
            checklist));
    }
}
