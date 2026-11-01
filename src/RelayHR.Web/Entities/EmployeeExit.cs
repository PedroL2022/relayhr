namespace RelayHR.Web.Entities;

/// <summary>
/// Salida registrada de un empleado (HU-01).
/// Una salida puede originar como máximo una Vacante Express.
/// </summary>
public class EmployeeExit
{
    public int EmployeeExitId { get; set; }

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public DateOnly ExitDate { get; set; }

    public string? Reason { get; set; }

    public DateTime CreatedAt { get; set; }

    // Vacante generada a partir de esta salida (0..1).
    public ExpressVacancy? ExpressVacancy { get; set; }
}
