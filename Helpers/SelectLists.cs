using Microsoft.AspNetCore.Mvc.Rendering;
using RelayHR.Web.Repository;

namespace RelayHR.Web.Helpers;

/// <summary>
/// Listas desplegables que se repiten en varios formularios.
/// </summary>
public static class SelectLists
{
    public static async Task<List<SelectListItem>> ToSelectListAsync(this IDepartmentRepository departments)
    {
        var items = await departments.GetAllOrderedAsync();
        return items.Select(d => new SelectListItem(d.Name, d.DepartmentId.ToString())).ToList();
    }

    public static async Task<List<SelectListItem>> ToSelectListAsync(this IPositionRepository positions)
    {
        var items = await positions.GetAllWithDepartmentAsync();
        return items.Select(p => new SelectListItem(p.Title, p.PositionId.ToString())).ToList();
    }

    /// <summary>Empleados activos, opcionalmente sin incluir a uno (por ejemplo, el que se va).</summary>
    public static async Task<List<SelectListItem>> ToSelectListAsync(this IEmployeeRepository employees, int? exceptEmployeeId = null)
    {
        var items = await employees.GetActiveAsync(exceptEmployeeId);
        return items.Select(e => new SelectListItem(e.FullName, e.EmployeeId.ToString())).ToList();
    }
}
