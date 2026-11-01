using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RelayHR.Web.Data;
using RelayHR.Web.Dtos;
using RelayHR.Web.Entities;
using RelayHR.Web.Security;
namespace RelayHR.Web.Controllers;
public sealed class VacanciesController(AppDbContext db) : Controller
{
    [Authorize(Policy = Policies.RecruiterAccess)]
    public IActionResult Index() => View();
    [Authorize(Policy = Policies.RecruiterAccess)]
    public async Task<IActionResult> Review(int id, CancellationToken ct)
    {
        if (!await db.ExpressVacancies.AnyAsync(v => v.ExpressVacancyId == id, ct)) return NotFound();
        return View(id);
    }
    [AllowAnonymous, HttpGet("vacancies/{id:int}")]
    public async Task<IActionResult> Public(int id, CancellationToken ct)
    {
        var vacancy = await db.ExpressVacancies.AsNoTracking().Include(v => v.Department)
            .SingleOrDefaultAsync(v => v.ExpressVacancyId == id && v.Status == ExpressVacancyStatus.Published, ct);
        return vacancy is null ? NotFound() : View(PublicVacancyDTO.FromEntity(vacancy));
    }
}
