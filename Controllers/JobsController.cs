using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RelayHR.Web.Repository;

namespace RelayHR.Web.Controllers;

/// <summary>
/// Vacantes publicadas, visibles para cualquier candidato sin iniciar sesión.
/// Solo se muestran los datos del puesto: nunca quién fue el empleado que salió.
/// </summary>
[AllowAnonymous]
public class JobsController : Controller
{
    private readonly IVacancyRepository _vacancies;

    public JobsController(IVacancyRepository vacancies)
    {
        _vacancies = vacancies;
    }

    public async Task<IActionResult> Index() => View(await _vacancies.GetPublishedAsync());

    public async Task<IActionResult> Details(int id)
    {
        var vacancy = await _vacancies.GetPublishedByIdAsync(id);
        return vacancy is null ? NotFound() : View(vacancy);
    }
}
