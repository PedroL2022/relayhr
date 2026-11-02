using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RelayHR.Web.Entities;
using RelayHR.Web.Helpers;
using RelayHR.Web.Repository;
using RelayHR.Web.Security;
using RelayHR.Web.Services;
using RelayHR.Web.ViewModels;

namespace RelayHR.Web.Controllers;

/// <summary>
/// Bandeja del reclutador: las Vacantes Express generadas se revisan, se completan y se publican (HU-04 y HU-05).
/// </summary>
[Authorize(Roles = Roles.Company)]
public class VacanciesController : Controller
{
    private readonly IVacancyRepository _vacancyRepository;
    private readonly IDepartmentRepository _departments;
    private readonly VacancyService _vacancies;

    public VacanciesController(
        IVacancyRepository vacancyRepository,
        IDepartmentRepository departments,
        VacancyService vacancies)
    {
        _vacancyRepository = vacancyRepository;
        _departments = departments;
        _vacancies = vacancies;
    }

    /// <summary>Bandeja. Con ?status=PendingReview se filtran las que esperan revisión.</summary>
    public async Task<IActionResult> Index(VacancyStatus? status)
    {
        ViewBag.Status = status;
        return View(await _vacancyRepository.GetInboxAsync(status));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var model = await BuildAsync(id, null);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(VacancyEditViewModel model)
    {
        if (ModelState.IsValid)
        {
            var result = await _vacancies.UpdateAsync(
                model.ExpressVacancyId, model.JobTitle, model.DepartmentId, model.JobDescription, model.Requirements);

            if (result.Succeeded)
            {
                TempData["Success"] = "Cambios guardados.";
                return RedirectToAction(nameof(Edit), new { id = model.ExpressVacancyId });
            }

            ModelState.AddModelError(string.Empty, result.Error!);
        }

        var viewModel = await BuildAsync(model.ExpressVacancyId, model);
        return viewModel is null ? NotFound() : View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> Publish(int id)
    {
        var result = await _vacancies.PublishAsync(id, User.GetDisplayName());

        if (result.Succeeded)
        {
            TempData["Success"] = "La vacante fue publicada y ya la ven los candidatos.";
        }
        else
        {
            TempData["Error"] = result.Error;
        }

        return RedirectToAction(nameof(Edit), new { id });
    }

    // Carga la vacante para mostrarla. Si hay un formulario con errores se conservan sus valores.
    private async Task<VacancyEditViewModel?> BuildAsync(int id, VacancyEditViewModel? posted)
    {
        var vacancy = await _vacancyRepository.GetDetailAsync(id);

        if (vacancy is null)
        {
            return null;
        }

        return new VacancyEditViewModel
        {
            ExpressVacancyId = vacancy.ExpressVacancyId,
            JobTitle = posted is null ? vacancy.JobTitle : posted.JobTitle,
            DepartmentId = posted is null ? vacancy.DepartmentId : posted.DepartmentId,
            JobDescription = posted is null ? vacancy.JobDescription : posted.JobDescription,
            Requirements = posted is null ? vacancy.Requirements : posted.Requirements,
            Vacancy = vacancy,
            Departments = await _departments.ToSelectListAsync()
        };
    }
}
