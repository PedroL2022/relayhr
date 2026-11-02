using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RelayHR.Web.Repository;
using RelayHR.Web.Security;
using RelayHR.Web.Services;

namespace RelayHR.Web.Controllers;

/// <summary>
/// Solicitudes de servicio que envía la empresa (HU-07). La plataforma solo las registra:
/// no procesa pagos ni genera contratos.
/// </summary>
[Authorize(Roles = Roles.Company)]
public class ServiceRequestsController : Controller
{
    private readonly IServiceRequestRepository _requests;
    private readonly TalentService _talent;

    public ServiceRequestsController(IServiceRequestRepository requests, TalentService talent)
    {
        _requests = requests;
        _talent = talent;
    }

    public async Task<IActionResult> Index() =>
        View(await _requests.GetByRequesterAsync(User.GetUserId()));

    /// <summary>Un solo clic en "Solicitar servicio": la empresa se toma de la cuenta de quien envía.</summary>
    [HttpPost]
    public async Task<IActionResult> Create(int freelancerProfileId)
    {
        var result = await _talent.RequestServiceAsync(freelancerProfileId, User.GetUserId());

        if (result.Succeeded)
        {
            TempData["Success"] = "Solicitud enviada. Puedes seguir su estado aquí.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Error"] = result.Error;
        return RedirectToAction("Details", "Directory", new { id = freelancerProfileId });
    }

    [HttpPost]
    public async Task<IActionResult> Confirm(int id)
    {
        var result = await _talent.ConfirmAsync(id, User.GetUserId());

        if (result.Succeeded)
        {
            TempData["Success"] = "Servicio confirmado.";
        }
        else
        {
            TempData["Error"] = result.Error;
        }

        return RedirectToAction(nameof(Index));
    }
}
