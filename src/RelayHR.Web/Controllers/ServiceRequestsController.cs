using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RelayHR.Web.Models.DTOs.ServiceRequests;
using RelayHR.Web.Services.ServiceRequests;

namespace RelayHR.Web.Controllers;

// Sin [ApiController] a propósito: así los errores de validación salen con el
// envoltorio { success, message, data } y no con ProblemDetails.
[Route("api/service-requests")]
public class ServiceRequestsController : ControllerBase
{
    private readonly IServiceRequestService _service;

    public ServiceRequestsController(IServiceRequestService service) => _service = service;

    [HttpPost]
    [Authorize(Roles = "Company")]
    public async Task<IActionResult> Create([FromBody] CreateServiceRequestDTO? dto, CancellationToken ct)
    {
        if (!TryGetUserId(out var companyId)) return InvalidIdentity();
        if (dto is null || !ModelState.IsValid) return ValidationFailure();

        return ToActionResult(await _service.CreateAsync(companyId, dto, ct));
    }

    [HttpGet]
    [Authorize(Roles = "Company,Freelancer")]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return InvalidIdentity();
        return ToActionResult(await _service.GetMineAsync(userId, User.IsInRole("Freelancer"), ct));
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "Company,Freelancer")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return InvalidIdentity();
        return ToActionResult(await _service.GetByIdAsync(userId, User.IsInRole("Freelancer"), id, ct));
    }

    [HttpPatch("{id:int}/respond")]
    [Authorize(Roles = "Freelancer")]
    public async Task<IActionResult> Respond(int id, [FromBody] RespondServiceRequestDTO? dto, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return InvalidIdentity();
        if (dto is null || !ModelState.IsValid) return ValidationFailure();

        return ToActionResult(await _service.RespondAsync(userId, id, dto.Response, ct));
    }

    [HttpPost("{id:int}/confirm")]
    [Authorize(Roles = "Company")]
    public async Task<IActionResult> Confirm(int id, CancellationToken ct)
    {
        if (!TryGetUserId(out var companyId)) return InvalidIdentity();
        return ToActionResult(await _service.ConfirmAsync(companyId, id, ct));
    }

    // ----------------------------------------------------------------- helpers

    // CompanyId = id del usuario autenticado. Si HU-08 maneja una tabla Company aparte,
    // cambia SOLO esta línea para leer el claim "companyId".
    private bool TryGetUserId(out int userId) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

    private IActionResult InvalidIdentity() =>
        StatusCode(StatusCodes.Status401Unauthorized, new ServiceRequestApiResponse<object>
        {
            Success = false,
            Message = "No se pudo identificar al usuario autenticado."
        });

    private IActionResult ValidationFailure()
    {
        var errors = ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .ToDictionary(
                e => e.Key,
                e => e.Value!.Errors
                      .Select(x => string.IsNullOrWhiteSpace(x.ErrorMessage) ? "Valor inválido." : x.ErrorMessage)
                      .ToArray());

        return BadRequest(new ServiceRequestApiResponse<object>
        {
            Success = false,
            Message = "Los datos enviados no son válidos.",
            Data = errors
        });
    }

    private IActionResult ToActionResult<T>(ServiceRequestResult<T> result)
    {
        var body = new ServiceRequestApiResponse<T>
        {
            Success = result.IsSuccess,
            Message = result.Message,
            Data = result.Data
        };

        var code = result.Status switch
        {
            ServiceRequestResultStatus.Ok => StatusCodes.Status200OK,
            ServiceRequestResultStatus.Created => StatusCodes.Status201Created,
            ServiceRequestResultStatus.BadRequest => StatusCodes.Status400BadRequest,
            ServiceRequestResultStatus.Forbidden => StatusCodes.Status403Forbidden,
            ServiceRequestResultStatus.NotFound => StatusCodes.Status404NotFound,
            ServiceRequestResultStatus.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        return StatusCode(code, body);
    }
}