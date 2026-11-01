namespace RelayHR.Web.Models;

/// <summary>Información de una tarea del checklist, incluido su responsable.</summary>
public sealed record ChecklistTaskResponseDTO(
    int Id,
    string Title,
    string Department,
    int ResponsibleUserId,
    string ResponsibleName,
    DateOnly DueDate,
    string Status);
