using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RelayHR.Web.Entities;
using RelayHR.Web.Helpers;
using RelayHR.Web.Repository;
using RelayHR.Web.Security;
using RelayHR.Web.ViewModels;

namespace RelayHR.Web.Controllers;

/// <summary>
/// Perfiles de puesto. Con estos datos se genera la Vacante Express cuando alguien se va.
/// </summary>
[Authorize(Roles = Roles.HRAdmin)]
public class PositionsController : Controller
{
    private readonly IPositionRepository _positions;
    private readonly IDepartmentRepository _departments;
    private readonly IUnitOfWork _unitOfWork;

    public PositionsController(
        IPositionRepository positions,
        IDepartmentRepository departments,
        IUnitOfWork unitOfWork)
    {
        _positions = positions;
        _departments = departments;
        _unitOfWork = unitOfWork;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _positions.GetAllWithDepartmentAsync());
    }

    [HttpGet]
    public async Task<IActionResult> Create() => View(await BuildFormAsync(new PositionFormViewModel()));

    [HttpPost]
    public async Task<IActionResult> Create(PositionFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(await BuildFormAsync(model));
        }

        _positions.Add(new Position
        {
            Title = model.Title.Trim(),
            DepartmentId = model.DepartmentId,
            Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
            Requirements = string.IsNullOrWhiteSpace(model.Requirements) ? null : model.Requirements.Trim()
        });
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = "Puesto registrado.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<PositionFormViewModel> BuildFormAsync(PositionFormViewModel model)
    {
        model.Departments = await _departments.ToSelectListAsync();
        return model;
    }
}
