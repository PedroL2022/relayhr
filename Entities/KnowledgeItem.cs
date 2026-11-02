namespace RelayHR.Web.Entities;

/// <summary>
/// Proyecto o tarea pendiente del empleado que sale (HU-03). Se documenta para que el
/// conocimiento no se pierda y se asigna a un responsable temporal.
/// </summary>
public class KnowledgeItem
{
    public int KnowledgeItemId { get; set; }

    public int EmployeeExitId { get; set; }
    public EmployeeExit EmployeeExit { get; set; } = null!;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    // Estado actual del trabajo, por ejemplo "En desarrollo, 60 % avanzado".
    public string CurrentStatus { get; set; } = string.Empty;

    // Responsable temporal. Si es null, la tarea se muestra como "sin asignar".
    public int? AssignedToEmployeeId { get; set; }
    public Employee? AssignedTo { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsUnassigned => AssignedToEmployeeId is null;
}
