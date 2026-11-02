using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RelayHR.Web.Repository;
using RelayHR.Web.Security;
using RelayHR.Web.Services;
using RelayHR.Web.ViewModels;

namespace RelayHR.Web.Controllers;

/// <summary>
/// Directorio de trabajadores independientes para cubrir necesidades temporales (HU-06).
/// </summary>
[Authorize(Roles = Roles.Company)]
public class DirectoryController : Controller
{
    private readonly IFreelancerRepository _profiles;
    private readonly IServiceRequestRepository _requests;
    private readonly TalentService _talent;

    public DirectoryController(IFreelancerRepository profiles, IServiceRequestRepository requests, TalentService talent)
    {
        _profiles = profiles;
        _requests = requests;
        _talent = talent;
    }

    public async Task<IActionResult> Index(DirectoryViewModel filter)
    {
        if (ModelState.IsValid)
        {
            filter.Results = await _talent.SearchAsync(filter.Category, filter.MaxHourlyRate, filter.OnlyAvailable);
        }

        return View(filter);
    }

    public async Task<IActionResult> Details(int id)
    {
        var profile = await _profiles.GetDetailAsync(id);

        if (profile is null)
        {
            return NotFound();
        }

        // Para avisar si ya hay una solicitud en curso con este profesional.
        ViewBag.HasActiveRequest = await _requests.HasActiveAsync(id, User.GetUserId());

        return View(profile);
    }
}
