using RelayHR.Web.Entities.Stubs;

namespace RelayHR.Web.Entities.KnowledgeTransfer;

public class KnowledgeTransferProject
{
    public int Id { get; set; }

    public int EmployeeExitId { get; set; }
    public EmployeeExit? EmployeeExit { get; set; }

    /// <summary>Empleado saliente dueño del proyecto (se copia desde la salida).</summary>
    public int DepartingEmployeeId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public KnowledgeTransferStatus Status { get; set; } = KnowledgeTransferStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<KnowledgeTransferTask> Tasks { get; set; } = new List<KnowledgeTransferTask>();
}
