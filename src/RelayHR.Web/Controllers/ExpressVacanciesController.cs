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
    private const int MaxPageSize = 100;

    private readonly AppDbContext _db;

    public ExpressVacanciesController(AppDbContext db)
    {
        _db = db;
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
            .Include(v => v.Department)
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
            .Include(v => v.Department)
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
