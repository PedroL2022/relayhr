using Microsoft.AspNetCore.Identity;

namespace RelayHR.Web.Entities;

public class User : IdentityUser<int>
{
    public string Name { get; set; } = string.Empty;

    public ICollection<ChecklistTask> AssignedTasks { get; set; } = new List<ChecklistTask>();
}
