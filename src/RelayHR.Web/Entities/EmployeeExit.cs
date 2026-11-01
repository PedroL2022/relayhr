namespace RelayHR.Web.Entities;

public class EmployeeExit
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public DateOnly EffectiveDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string Status { get; set; } = EmployeeExitStatuses.InProgress;

    public Employee Employee { get; set; } = null!;

    public ExitChecklist Checklist { get; set; } = null!;
}

public static class EmployeeExitStatuses
{
    public const string Pending = "Pending";
    public const string InProgress = "InProgress";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";
}
