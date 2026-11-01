namespace RelayHR.Web.Entities;

public class ExitChecklist
{
    public int Id { get; set; }

    public int EmployeeExitId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public EmployeeExit EmployeeExit { get; set; } = null!;

    public ICollection<ChecklistTask> Tasks { get; set; } = new List<ChecklistTask>();
}
