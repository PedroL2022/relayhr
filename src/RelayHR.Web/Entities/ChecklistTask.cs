namespace RelayHR.Web.Entities;

public class ChecklistTask
{
    public int Id { get; set; }

    public int ExitChecklistId { get; set; }

    public int DepartmentId { get; set; }

    public string Title { get; set; } = string.Empty;

    public int ResponsibleUserId { get; set; }

    public DateOnly DueDate { get; set; }

    public ChecklistTaskStatus Status { get; set; } = ChecklistTaskStatus.Pending;

    public ExitChecklist ExitChecklist { get; set; } = null!;

    public Department Department { get; set; } = null!;

    public User ResponsibleUser { get; set; } = null!;
}
