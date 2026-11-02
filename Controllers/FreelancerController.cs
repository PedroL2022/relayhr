using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RelayHR.Web.Repository;
using RelayHR.Web.Security;
using RelayHR.Web.Services;
using RelayHR.Web.ViewModels;

namespace RelayHR.Web.Controllers;

/// <summary>
/// Área del trabajador independiente: administra su perfil y responde las solicitudes que recibe.
/// </summary>
[Authorize(Roles = Roles.Freelancer)]
public class FreelancerController : Controller
{
    private readonly IFreelancerRepository _profiles;
    private readonly IServiceRequestRepository _requests;
    private readonly TalentService _talent;

    public FreelancerController(IFreelancerRepository profiles, IServiceRequestRepository requests, TalentService talent)
    {
        _profiles = profiles;
        _requests = requests;
        _talent = talent;
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var userId = User.GetUserId();
        var profile = await _profiles.GetByUserIdReadOnlyAsync(userId);

        if (profile is null)
        {
            return View(new FreelancerProfileViewModel());
        }

        return View(new FreelancerProfileViewModel
        {
            Category = profile.Category,
            ExperienceYears = profile.ExperienceYears,
            ExperienceSummary = profile.ExperienceSummary,
            PortfolioUrl = profile.PortfolioUrl,
            HourlyRate = profile.HourlyRate,
            IsAvailable = profile.IsAvailable,
            AvailabilityNote = profile.AvailabilityNote
        });
    }

    [HttpPost]
    public async Task<IActionResult> Profile(FreelancerProfileViewModel model)
    {
        if (ModelState.IsValid)
        {
            var result = await _talent.SaveProfileAsync(
                User.GetUserId(), model.Category, model.ExperienceYears, model.ExperienceSummary,
                model.PortfolioUrl, model.HourlyRate, model.IsAvailable, model.AvailabilityNote);

            if (result.Succeeded)
            {
                TempData["Success"] = "Tu perfil fue guardado.";
                return RedirectToAction(nameof(Profile));
            }

            ModelState.AddModelError(string.Empty, result.Error!);
        }

        return View(model);
    }

    public async Task<IActionResult> Requests()
    {
        return View(await _requests.GetByFreelancerUserAsync(User.GetUserId()));
    }

    [HttpPost]
    public async Task<IActionResult> Respond(int id, bool accept)
    {
        var result = await _talent.RespondAsync(id, User.GetUserId(), accept);

        if (result.Succeeded)
        {
            TempData["Success"] = accept ? "Aceptaste la solicitud." : "Rechazaste la solicitud.";
        }
        else
        {
            TempData["Error"] = result.Error;
        }

        return RedirectToAction(nameof(Requests));
    }
}
