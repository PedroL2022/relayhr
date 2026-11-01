using System.Security.Claims;
using RelayHR.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RelayHR.Web.Data;
using RelayHR.Web.Dtos;
using RelayHR.Web.Entities;
using RelayHR.Web.Security;

namespace RelayHR.Web.Controllers;

/// <summary>
/// Bandeja del reclutador: consulta de Vacantes Express. Solo lectura.
/// </summary>
[ApiController]
[Route("api/express-vacancies")]
[Authorize(Policy = Policies.RecruiterAccess)]
public class ExpressVacanciesController : ControllerBase
{
    [HttpPut("{id:int}")]
    [VacancyAntiforgery]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateExpressVacancyDTO dto, CancellationToken ct)
    {
        if (!TryUserId(out var userId)) return Unauthorized(ApiResponse<object>.Fail("Identidad no válida."));
        var result = await _publication.UpdateAsync(id, userId, dto, ct);
        return StatusCode(result.StatusCode, result.Body);
    }

    [HttpPost("{id:int}/publish")]
    [Authorize(Policy = Policies.PublishVacancy)]
    [VacancyAntiforgery]
    public async Task<IActionResult> Publish(int id, CancellationToken ct)
    {
        if (!TryUserId(out var userId)) return Unauthorized(ApiResponse<object>.Fail("Identidad no válida."));
        var result = await _publication.PublishAsync(id, userId, ct);
        return StatusCode(result.StatusCode, result.Body);
    }

    [HttpGet("departments")]
    public async Task<IActionResult> Departments(CancellationToken ct) => Ok(await _db.Departments
        .AsNoTracking().OrderBy(d => d.Name).Select(d => new { d.Id, d.Name }).ToListAsync(ct));

    private bool TryUserId(out int userId) => int.TryParse(
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out userId) && userId > 0;

    private const int MaxPageSize = 100;

    private readonly AppDbContext _db;
    private readonly ExpressVacancyPublicationService _publication;

    public ExpressVacanciesController(AppDbContext db, ExpressVacancyPublicationService publication)
    {
        _db = db;
        _publication = publication;
    }

    /// <summary>
    /// Lista las vacantes (más recientes primero). Con ?status=PendingReview se filtra la bandeja de pendientes.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<ExpressVacancyResponseDTO>>>> GetAll(
        [FromQuery] ExpressVacancyStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (status.HasValue && !Enum.IsDefined(status.Value))
        {
            return BadRequest(ApiResponse<PagedResult<ExpressVacancyResponseDTO>>.Fail("El estado indicado no es válido."));
        }

        if (page < 1 || pageSize < 1 || pageSize > MaxPageSize)
        {
            return BadRequest(ApiResponse<PagedResult<ExpressVacancyResponseDTO>>.Fail(
                $"La página debe ser 1 o mayor y el tamaño de página debe estar entre 1 y {MaxPageSize}."));
        }

        // Solo lectura: AsNoTracking para no modificar ni rastrear los registros.
        var query = _db.ExpressVacancies
            .AsNoTracking()
            .Include(v => v.Department).Include(v => v.EmployeeExit)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(v => v.Status == status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var vacancies = await query
            .OrderByDescending(v => v.CreatedAt)
            .ThenByDescending(v => v.ExpressVacancyId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var result = new PagedResult<ExpressVacancyResponseDTO>
        {
            Items = vacancies.Select(ExpressVacancyResponseDTO.FromEntity).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };

        return Ok(ApiResponse<PagedResult<ExpressVacancyResponseDTO>>.Ok(result, "Vacantes Express obtenidas correctamente."));
    }

    /// <summary>
    /// Devuelve el detalle de una vacante, incluida la salida que la originó y los campos pendientes.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<ExpressVacancyResponseDTO>>> GetById(
        int id,
        CancellationToken cancellationToken = default)
    {
        var vacancy = await _db.ExpressVacancies
            .AsNoTracking()
            .Include(v => v.Department).Include(v => v.EmployeeExit)
            .FirstOrDefaultAsync(v => v.ExpressVacancyId == id, cancellationToken);

        if (vacancy is null)
        {
            return NotFound(ApiResponse<ExpressVacancyResponseDTO>.Fail("Vacante Express no encontrada."));
        }

        return Ok(ApiResponse<ExpressVacancyResponseDTO>.Ok(
            ExpressVacancyResponseDTO.FromEntity(vacancy),
            "Vacante Express obtenida correctamente."));
    }
}
