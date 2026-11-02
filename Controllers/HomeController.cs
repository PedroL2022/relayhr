using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RelayHR.Web.Models;
using RelayHR.Web.Repository;
using RelayHR.Web.Security;
using RelayHR.Web.ViewModels;

namespace RelayHR.Web.Controllers;

[AllowAnonymous]
public class HomeController : Controller
{
    private readonly IExitRepository _exits;
    private readonly IKnowledgeItemRepository _items;
    private readonly IVacancyRepository _vacancies;
    private readonly IServiceRequestRepository _requests;
    private readonly IFreelancerRepository _profiles;

    public HomeController(
        IExitRepository exits,
        IKnowledgeItemRepository items,
        IVacancyRepository vacancies,
        IServiceRequestRepository requests,
        IFreelancerRepository profiles)
    {
        _exits = exits;
        _items = items;
        _vacancies = vacancies;
        _requests = requests;
        _profiles = profiles;
    }

    public async Task<IActionResult> Index()
    {
        var model = new DashboardViewModel();

        if (User.IsInRole(Roles.HRAdmin))
        {
            model.ExitsInProgress = await _exits.CountInProgressAsync();
            model.UnassignedItems = await _items.CountUnassignedAsync();
        }

        if (Roles.IsCompanyRole(User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value))
        {
            model.VacanciesToPublish = await _vacancies.CountNotPublishedAsync();
            model.ActiveRequests = await _requests.CountActiveByRequesterAsync(User.GetUserId());
        }

        if (User.IsInRole(Roles.Freelancer))
        {
            var userId = User.GetUserId();
            model.FreelancerHasProfile = await _profiles.ExistsForUserAsync(userId);
            model.IncomingRequests = await _requests.CountSentForFreelancerAsync(userId);
        }

        return View(model);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
