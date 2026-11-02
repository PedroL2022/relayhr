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
/// Salidas de empleados: registro con checklist automático (HU-01) y control de cierre (HU-02).
/// </summary>
[Authorize(Roles = Roles.HRAdmin)]
public class ExitsController : Controller
{
    private readonly IExitRepository _exitRepository;
    private readonly IEmployeeRepository _employees;
    private readonly ExitService _exits;

    public ExitsController(IExitRepository exitRepository, IEmployeeRepository employees, ExitService exits)
    {
        _exitRepository = exitRepository;
        _employees = employees;
        _exits = exits;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _exitRepository.GetAllWithTasksAsync());
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new ExitFormViewModel
        {
            ExitDate = DateOnly.FromDateTime(DateTime.Today),
            Employees = await _employees.ToSelectListAsync()
        };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(ExitFormViewModel model)
    {
        if (ModelState.IsValid)
        {
            var result = await _exits.RegisterAsync(
                model.EmployeeId!.Value, model.ExitDate!.Value, model.Reason, User.GetDisplayName());

            if (result.Succeeded)
            {
                TempData["Success"] = "Salida registrada. Se generó el checklist y la Vacante Express de reemplazo.";
                return RedirectToAction(nameof(Details), new { id = result.Value });
            }

            ModelState.AddModelError(string.Empty, result.Error!);
        }

        model.Employees = await _employees.ToSelectListAsync();
        return View(model);
    }

    public async Task<IActionResult> Details(int id)
    {
        var exit = await _exitRepository.GetDetailsAsync(id);

        return exit is null ? NotFound() : View(exit);
    }

    [HttpPost]
    public async Task<IActionResult> SetTaskStatus(int exitId, int taskId, ExitTaskStatus status)
    {
        var result = await _exits.SetTaskStatusAsync(taskId, status, User.GetDisplayName());
        Notify(result, "Tarea actualizada.");
        return RedirectToAction(nameof(Details), new { id = exitId });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateTask(int exitId, int taskId, string responsibleName, DateOnly dueDate)
    {
        var result = await _exits.UpdateTaskAsync(taskId, responsibleName, dueDate);
        Notify(result, "Responsable y fecha límite actualizados.");
        return RedirectToAction(nameof(Details), new { id = exitId });
    }

    [HttpPost]
    public async Task<IActionResult> Close(int id)
    {
        var result = await _exits.CloseAsync(id, User.GetDisplayName());
        Notify(result, "La salida fue cerrada correctamente.");
        return RedirectToAction(nameof(Details), new { id });
    }

    private void Notify(Result result, string successMessage)
    {
        if (result.Succeeded)
        {
            TempData["Success"] = successMessage;
        }
        else
        {
            TempData["Error"] = result.Error;
        }
    }
}
