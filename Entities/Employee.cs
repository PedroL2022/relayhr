namespace RelayHR.Web.Entities;

/// <summary>
/// Empleado de la organización.
/// </summary>
public class Employee
{
    public int EmployeeId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public int? PositionId { get; set; }
    public Position? Position { get; set; }

    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }

    // Pasa a false cuando se registra su salida.
    public bool IsActive { get; set; } = true;
}
