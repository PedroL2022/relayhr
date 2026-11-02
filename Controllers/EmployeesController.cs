using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RelayHR.Web.Entities;
using RelayHR.Web.Helpers;
using RelayHR.Web.Repository;
using RelayHR.Web.Security;
using RelayHR.Web.ViewModels;

namespace RelayHR.Web.Controllers;

/// <summary>
/// Empleados de la empresa. Son los que luego pueden tener una salida registrada.
/// </summary>
[Authorize(Roles = Roles.HRAdmin)]
public class EmployeesController : Controller
{
    private readonly IEmployeeRepository _employees;
    private readonly IPositionRepository _positions;
    private readonly IDepartmentRepository _departments;
    private readonly IUnitOfWork _unitOfWork;

    public EmployeesController(
        IEmployeeRepository employees,
        IPositionRepository positions,
        IDepartmentRepository departments,
        IUnitOfWork unitOfWork)
    {
        _employees = employees;
        _positions = positions;
        _departments = departments;
        _unitOfWork = unitOfWork;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _employees.GetAllWithDetailsAsync());
    }

    [HttpGet]
    public async Task<IActionResult> Create() => View(await BuildFormAsync(new EmployeeFormViewModel()));

    [HttpPost]
    public async Task<IActionResult> Create(EmployeeFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(await BuildFormAsync(model));
        }

        _employees.Add(new Employee
        {
            FullName = model.FullName.Trim(),
            PositionId = model.PositionId,
            DepartmentId = model.DepartmentId,
            IsActive = true
        });
        await _unitOfWork.SaveChangesAsync();

        TempData["Success"] = "Empleado registrado.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<EmployeeFormViewModel> BuildFormAsync(EmployeeFormViewModel model)
    {
        model.Positions = await _positions.ToSelectListAsync();
        model.Departments = await _departments.ToSelectListAsync();
        return model;
    }
}
