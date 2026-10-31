using RelayHR.Web.Entities.Stubs;

namespace RelayHR.Web.Entities.KnowledgeTransfer;

public class KnowledgeTransferTask
{
    public int Id { get; set; }

    public int EmployeeExitId { get; set; }
    public EmployeeExit? EmployeeExit { get; set; }

    /// <summary>Proyecto al que pertenece la tarea. Null = tarea independiente.</summary>
    public int? ProjectId { get; set; }
    public KnowledgeTransferProject? Project { get; set; }

    /// <summary>Empleado saliente dueño de la tarea (se copia desde la salida).</summary>
    public int DepartingEmployeeId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public KnowledgeTransferStatus Status { get; set; } = KnowledgeTransferStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // SCRUM-26 agrega aquí: TemporaryResponsibleUserId y AssignedAt.
}
