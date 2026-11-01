namespace RelayHR.Web.Entities;

public class Employee
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<EmployeeExit> Exits { get; set; } = new List<EmployeeExit>();
}
