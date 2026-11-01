namespace RelayHR.Web.Entities;

public class Department
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<ChecklistTask> ChecklistTasks { get; set; } = new List<ChecklistTask>();
}
