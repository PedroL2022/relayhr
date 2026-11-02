namespace RelayHR.Web.Entities;

public enum ExitStatus
{
    InProgress = 1,
    Closed = 2
}

/// <summary>
/// Salida registrada de un empleado (HU-01). Es el punto de partida de todo el proceso:
/// genera su checklist, su traspaso de conocimiento y su Vacante Express.
/// </summary>
public class EmployeeExit
{
    public int EmployeeExitId { get; set; }

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public DateOnly ExitDate { get; set; }

    public string? Reason { get; set; }

    public ExitStatus Status { get; set; } = ExitStatus.InProgress;

    // Momento del registro. Se usa para medir la meta de publicar la vacante en menos de 48 horas.
    public DateTime CreatedAt { get; set; }

    public string RegisteredBy { get; set; } = string.Empty;

    public DateTime? ClosedAt { get; set; }
    public string? ClosedBy { get; set; }

    public List<ExitTask> Tasks { get; set; } = new();
    public List<KnowledgeItem> KnowledgeItems { get; set; } = new();

    // Vacante generada a partir de esta salida (0..1).
    public ExpressVacancy? ExpressVacancy { get; set; }

    /// <summary>Crea las tareas del checklist a partir de la plantilla (HU-01).</summary>
    public void GenerateChecklist()
    {
        foreach (var item in ExitChecklistTemplate.Items)
        {
            Tasks.Add(new ExitTask
            {
                EmployeeExit = this,
                Title = item.Title,
                Area = item.Area,
                ResponsibleName = DefaultResponsible(item.Area),
                DueDate = ExitDate.AddDays(item.DueOffsetDays),
                IsMandatory = item.Mandatory,
                Kind = item.Kind,
                Status = ExitTaskStatus.Pending
            });
        }
    }

    /// <summary>
    /// Intenta cerrar la salida (HU-02). Solo se cierra si todas las tareas obligatorias están
    /// completas. Si no, devuelve false y la lista de tareas obligatorias pendientes.
    /// </summary>
    public bool TryClose(string closedBy, DateTime now, out List<ExitTask> pendingMandatory)
    {
        pendingMandatory = GetPendingMandatoryTasks();

        if (Status == ExitStatus.Closed || pendingMandatory.Count > 0)
        {
            return false;
        }

        Status = ExitStatus.Closed;
        ClosedAt = now;
        ClosedBy = closedBy;
        return true;
    }

    // Se usan métodos (y no propiedades) para las listas de entidades, así EF Core no las confunde con relaciones.
    public List<ExitTask> GetPendingMandatoryTasks() =>
        Tasks.Where(t => t.IsMandatory && t.Status != ExitTaskStatus.Completed).ToList();

    /// <summary>True si todas las tareas del tipo indicado (equipos, accesos) están completadas.</summary>
    public bool IsKindCompleted(ExitTaskKind kind)
    {
        var tasks = Tasks.Where(t => t.Kind == kind).ToList();
        return tasks.Count > 0 && tasks.All(t => t.Status == ExitTaskStatus.Completed);
    }

    public int ProgressPercent =>
        Tasks.Count == 0
            ? 0
            : (int)Math.Round(100.0 * Tasks.Count(t => t.Status == ExitTaskStatus.Completed) / Tasks.Count);

    public static string DefaultResponsible(ExitArea area) => area switch
    {
        ExitArea.Technology => "Equipo de Tecnología",
        ExitArea.HumanResources => "Equipo de Recursos Humanos",
        ExitArea.Finance => "Equipo de Finanzas",
        _ => "Sin asignar"
    };
}
