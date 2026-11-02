namespace RelayHR.Web.Entities;

/// <summary>Área de la empresa responsable de una tarea del checklist de salida.</summary>
public enum ExitArea
{
    Technology = 1,
    HumanResources = 2,
    Finance = 3
}

public enum ExitTaskStatus
{
    Pending = 1,
    InProgress = 2,
    Completed = 3
}

/// <summary>
/// Tipo especial de tarea. Permite mostrar "equipos devueltos" y "accesos revocados"
/// y usar el texto correcto en el botón (HU-02).
/// </summary>
public enum ExitTaskKind
{
    General = 1,
    EquipmentReturn = 2,
    AccessRevocation = 3
}

/// <summary>
/// Tarea del checklist de salida (HU-01). Si es obligatoria, la salida no se puede cerrar
/// hasta que esté completada (HU-02).
/// </summary>
public class ExitTask
{
    public int ExitTaskId { get; set; }

    public int EmployeeExitId { get; set; }
    public EmployeeExit EmployeeExit { get; set; } = null!;

    public string Title { get; set; } = string.Empty;

    public ExitArea Area { get; set; }

    public string ResponsibleName { get; set; } = string.Empty;

    public DateOnly DueDate { get; set; }

    public ExitTaskStatus Status { get; set; } = ExitTaskStatus.Pending;

    public bool IsMandatory { get; set; }

    public ExitTaskKind Kind { get; set; } = ExitTaskKind.General;

    // Quién y cuándo la completó (HU-02, criterio 3).
    public DateTime? CompletedAt { get; set; }
    public string? CompletedBy { get; set; }

    /// <summary>Cambia el estado y deja registrado quién y cuándo la completó.</summary>
    public void SetStatus(ExitTaskStatus status, string userName, DateTime now)
    {
        Status = status;

        if (status == ExitTaskStatus.Completed)
        {
            CompletedAt = now;
            CompletedBy = userName;
        }
        else
        {
            CompletedAt = null;
            CompletedBy = null;
        }
    }

    public bool IsOverdue(DateOnly today) =>
        Status != ExitTaskStatus.Completed && DueDate < today;
}

/// <summary>
/// Plantilla del checklist que se genera automáticamente al registrar una salida:
/// tareas por departamento (Tecnología, Recursos Humanos y Finanzas).
/// </summary>
public static class ExitChecklistTemplate
{
    public record Item(ExitArea Area, string Title, bool Mandatory, ExitTaskKind Kind, int DueOffsetDays);

    public static readonly IReadOnlyList<Item> Items = new List<Item>
    {
        new(ExitArea.Technology, "Devolver laptop, celular y demás equipos", true, ExitTaskKind.EquipmentReturn, 0),
        new(ExitArea.Technology, "Revocar accesos a sistemas, correo y VPN", true, ExitTaskKind.AccessRevocation, 0),
        new(ExitArea.Technology, "Respaldar los archivos del empleado", false, ExitTaskKind.General, 2),

        new(ExitArea.HumanResources, "Recuperar carnet y credenciales físicas", true, ExitTaskKind.General, 0),
        new(ExitArea.HumanResources, "Entregar la constancia de salida", true, ExitTaskKind.General, 3),
        new(ExitArea.HumanResources, "Realizar la entrevista de salida", false, ExitTaskKind.General, 3),

        new(ExitArea.Finance, "Confirmar anticipos y gastos pendientes", true, ExitTaskKind.General, 2),
        new(ExitArea.Finance, "Cancelar la tarjeta corporativa", true, ExitTaskKind.General, 1)
    };
}
