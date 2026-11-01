using RelayHR.Web.Models;

namespace RelayHR.Web.Services;

public interface IChecklistTaskService
{
    /// <summary>Actualiza el estado de una tarea aplicando las reglas del checklist.</summary>
    Task<ChecklistTaskStatusResponse> UpdateStatusAsync(
        int taskId,
        UpdateChecklistTaskStatusDTO request,
        int userId,
        bool isHumanResources,
        CancellationToken cancellationToken);
}