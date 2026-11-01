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

    // --- Nuevos campos (SCRUM-22) ---

    /// <summary>Obligatoria (true) u opcional (false). Solo las obligatorias bloquean el cierre.</summary>
    public bool IsRequired { get; set; } = true;

    /// <summary>Fecha de finalización en UTC. La genera el servidor, nunca el cliente.</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>Usuario autenticado que completó la tarea.</summary>
    public int? CompletedByUserId { get; set; }

    /// <summary>Notas enviadas en el PATCH (por ejemplo, "Equipo devuelto y verificado.").</summary>
    public string? Notes { get; set; }

    public ExitChecklist ExitChecklist { get; set; } = null!;

    public Department Department { get; set; } = null!;

    public User ResponsibleUser { get; set; } = null!;

    public User? CompletedByUser { get; set; }
}