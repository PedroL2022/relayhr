namespace RelayHR.Web.Models;

/// <summary>Checklist asociado a una salida y todas sus tareas ordenadas.</summary>
public sealed record ExitChecklistResponseDTO(
    int ChecklistId,
    int EmployeeExitId,
    IReadOnlyList<ChecklistTaskResponseDTO> Tasks);
