namespace RelayHR.Web.Entities;

public class Employee
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int? PositionId { get; set; }
    public Position? Position { get; set; }
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }

    public ICollection<EmployeeExit> Exits { get; set; } = new List<EmployeeExit>();
}
