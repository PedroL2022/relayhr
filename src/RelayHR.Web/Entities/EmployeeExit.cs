namespace RelayHR.Web.Entities;

public class EmployeeExit
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public DateOnly EffectiveDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string Status { get; set; } = EmployeeExitStatuses.InProgress;

    // --- Cierre del proceso (SCRUM-23) ---

    /// <summary>Fecha de cierre en UTC. La genera el servidor.</summary>
    public DateTime? ClosedAt { get; set; }

    /// <summary>Usuario autenticado de Recursos Humanos que cerró el proceso.</summary>
    public int? ClosedByUserId { get; set; }

    public User? ClosedByUser { get; set; }

    public Employee Employee { get; set; } = null!;

    public ExitChecklist Checklist { get; set; } = null!;
}

public static class EmployeeExitStatuses
{
    public const string Pending = "Pending";
    public const string InProgress = "InProgress";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";
    public const string Closed = "Closed";
}