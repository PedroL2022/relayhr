using Microsoft.EntityFrameworkCore;
using RelayHR.Web.Data;
using RelayHR.Web.Entities;
using RelayHR.Web.Models;

namespace RelayHR.Web.Services;

public sealed class ChecklistTaskService(RelayHrDbContext db) : IChecklistTaskService
{
    public async Task<ChecklistTaskStatusResponse> UpdateStatusAsync(
        int taskId,
        UpdateChecklistTaskStatusDTO request,
        int userId,
        bool isHumanResources,
        CancellationToken cancellationToken)
    {
        if (!TryParseStatus(request.Status, out var newStatus))
        {
            throw new InvalidTaskStatusException(request.Status);
        }

        var task = await db.ChecklistTasks
            .Include(t => t.ExitChecklist)
                .ThenInclude(c => c.EmployeeExit)
            .FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken)
            ?? throw new ChecklistTaskNotFoundException(taskId);

        // Solo Recursos Humanos o el responsable asignado pueden actualizar la tarea.
        if (!isHumanResources && task.ResponsibleUserId != userId)
        {
            throw new TaskUpdateForbiddenException();
        }

        // Un proceso cerrado no admite cambios en sus tareas.
        if (task.ExitChecklist.EmployeeExit.Status == EmployeeExitStatuses.Closed)
        {
            throw new ClosedProcessException(
                "El proceso de salida está cerrado; no se pueden actualizar sus tareas.");
        }

        var wasCompleted = task.Status == ChecklistTaskStatus.Completed;

        if (newStatus == ChecklistTaskStatus.Completed)
        {
            // La fecha la genera el servidor en UTC y el usuario sale de la sesión,
            // nunca del cuerpo de la solicitud. Si ya estaba completada se conservan
            // los datos originales de finalización.
            if (!wasCompleted)
            {
                task.CompletedAt = DateTime.UtcNow;
                task.CompletedByUserId = userId;
            }
        }
        else if (wasCompleted)
        {
            // Al reabrir una tarea se limpian sus datos de finalización para que
            // no queden datos de una finalización que ya no es vigente.
            task.CompletedAt = null;
            task.CompletedByUserId = null;
        }

        task.Status = newStatus;
        if (request.Notes is not null)
        {
            task.Notes = request.Notes.Trim();
        }

        // IsRequired no se toca: la tarea conserva su clasificación.
        await db.SaveChangesAsync(cancellationToken);

        return new ChecklistTaskStatusResponse(
            task.Id,
            task.Title,
            task.Status.ToString(),
            task.IsRequired,
            task.CompletedAt,
            task.CompletedByUserId,
            task.Notes);
    }

    private static bool TryParseStatus(string? value, out ChecklistTaskStatus status)
    {
        status = default;

        // Se rechazan los números ("1") para que solo valgan los nombres del estado.
        if (string.IsNullOrWhiteSpace(value) || int.TryParse(value, out _))
        {
            return false;
        }

        return Enum.TryParse(value.Trim(), ignoreCase: true, out status)
            && Enum.IsDefined(status);
    }
}