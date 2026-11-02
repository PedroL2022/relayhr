using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RelayHR.Web.Helpers;
using RelayHR.Web.Repository;
using RelayHR.Web.Security;
using RelayHR.Web.Services;
using RelayHR.Web.ViewModels;

namespace RelayHR.Web.Controllers;

/// <summary>
/// Traspaso de conocimiento (HU-03): documentar proyectos y tareas pendientes del empleado
/// que se va y asignarlos a responsables temporales.
/// </summary>
[Authorize(Roles = Roles.HRAdmin)]
public class HandoverController : Controller
{
    private readonly IExitRepository _exitRepository;
    private readonly IKnowledgeItemRepository _items;
    private readonly IEmployeeRepository _employees;
    private readonly ExitService _exits;

    public HandoverController(
        IExitRepository exitRepository,
        IKnowledgeItemRepository items,
        IEmployeeRepository employees,
        ExitService exits)
    {
        _exitRepository = exitRepository;
        _items = items;
        _employees = employees;
        _exits = exits;
    }

    public async Task<IActionResult> Index(int exitId)
    {
        var model = await BuildAsync(exitId, new KnowledgeItemFormViewModel { ExitId = exitId });
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Add([Bind(Prefix = nameof(HandoverViewModel.Form))] KnowledgeItemFormViewModel form)
    {
        if (ModelState.IsValid)
        {
            var result = await _exits.AddKnowledgeItemAsync(
                form.ExitId, form.Title, form.Description, form.CurrentStatus, form.AssignedToEmployeeId);

            if (result.Succeeded)
            {
                TempData["Success"] = "Elemento documentado.";
                return RedirectToAction(nameof(Index), new { exitId = form.ExitId });
            }

            ModelState.AddModelError(string.Empty, result.Error!);
        }

        // Si algo falló, se vuelve a mostrar la página con el formulario y sus errores.
        var model = await BuildAsync(form.ExitId, form);
        return model is null ? NotFound() : View(nameof(Index), model);
    }

    [HttpPost]
    public async Task<IActionResult> Assign(int exitId, int itemId, int? employeeId)
    {
        var result = await _exits.AssignKnowledgeItemAsync(itemId, employeeId);

        if (result.Succeeded)
        {
            TempData["Success"] = employeeId is null ? "Se quitó el responsable temporal." : "Responsable temporal asignado.";
        }
        else
        {
            TempData["Error"] = result.Error;
        }

        return RedirectToAction(nameof(Index), new { exitId });
    }

    /// <summary>Lista de lo que tiene a cargo un empleado como responsable temporal.</summary>
    public async Task<IActionResult> ByAssignee(int employeeId)
    {
        var employee = await _employees.GetReadOnlyAsync(employeeId);
        if (employee is null)
        {
            return NotFound();
        }

        var items = await _items.GetByAssigneeAsync(employeeId);

        return View(new AssigneeViewModel { Employee = employee, Items = items });
    }

    private async Task<HandoverViewModel?> BuildAsync(int exitId, KnowledgeItemFormViewModel form)
    {
        var exit = await _exitRepository.GetWithEmployeeAsync(exitId);

        if (exit is null)
        {
            return null;
        }

        var items = await _items.GetByExitAsync(exitId);

        return new HandoverViewModel
        {
            Exit = exit,
            Items = items,
            Employees = await _employees.ToSelectListAsync(exceptEmployeeId: exit.EmployeeId),
            Form = form
        };
    }
}
