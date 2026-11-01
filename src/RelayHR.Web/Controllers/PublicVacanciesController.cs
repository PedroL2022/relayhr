using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RelayHR.Web.Data;
using RelayHR.Web.Dtos;
using RelayHR.Web.Entities;
namespace RelayHR.Web.Controllers;
[ApiController, AllowAnonymous, Route("api/public/vacancies")]
public sealed class PublicVacanciesController(AppDbContext db) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        var vacancy = await db.ExpressVacancies.AsNoTracking().Include(v => v.Department)
            .SingleOrDefaultAsync(v => v.ExpressVacancyId == id && v.Status == ExpressVacancyStatus.Published, ct);
        return vacancy is null ? NotFound(ApiResponse<object>.Fail("Vacante no encontrada."))
            : Ok(ApiResponse<PublicVacancyDTO>.Ok(PublicVacancyDTO.FromEntity(vacancy), "Vacante publicada."));
    }
}
