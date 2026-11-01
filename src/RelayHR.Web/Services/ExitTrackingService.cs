using Microsoft.EntityFrameworkCore;
using RelayHR.Web.Data;
using RelayHR.Web.Entities;
using RelayHR.Web.Models;

namespace RelayHR.Web.Services;

public sealed class ExitNotFoundException(int exitId)
    : Exception($"No existe una salida con identificador {exitId}.");

public sealed record CloseResult(
    CloseExitResponse? Closed,
    List<PendingTaskDTO>? PendingTasks,
    string? BlockedMessage);

public interface IExitTrackingService
{
    Task<CloseResult> CloseAsync(int exitId, int userId, CancellationToken cancellationToken);

    Task<ExitProgressResponse> GetProgressAsync(int exitId, CancellationToken cancellationToken);
}

public sealed class ExitTrackingService(RelayHrDbContext db) : IExitTrackingService
{
    public async Task<ExitProgressResponse> GetProgressAsync(
        int exitId,
        CancellationToken cancellationToken)
    {
        if (!await db.EmployeeExits.AnyAsync(e => e.Id == exitId, cancellationToken))
        {
            throw new ExitNotFoundException(exitId);
        }

        var tasks = await db.ChecklistTasks
            .AsNoTracking()
            .Where(t => t.ExitChecklist.EmployeeExitId == exitId)
            .ToListAsync(cancellationToken);

        var p = ChecklistProgressCalculator.Calculate(tasks);
        return new ExitProgressResponse(
            exitId, p.Total, p.Completed, p.Pending, p.InProgress,
            p.Percentage, p.RequiredIncomplete, p.CanClose);
    }

    public async Task<CloseResult> CloseAsync(
        int exitId,
        int userId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var exit = await db.EmployeeExits
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == exitId, cancellationToken)
            ?? throw new ExitNotFoundException(exitId);

        if (exit.Status == EmployeeExitStatuses.Closed)
        {
            throw new ClosedProcessException("El proceso de salida ya está cerrado.");
        }

        var tasks = await db.ChecklistTasks
            .AsNoTracking()
            .Include(t => t.Department)
            .Where(t => t.ExitChecklist.EmployeeExitId == exitId)
            .ToListAsync(cancellationToken);

        var progress = ChecklistProgressCalculator.Calculate(tasks);
        if (!progress.CanClose)
        {
            var pending = tasks
                .Where(t => t.IsRequired && t.Status != ChecklistTaskStatus.Completed)
                .Select(t => new PendingTaskDTO(t.Id, t.Title, t.Department.Name, t.Status.ToString()))
                .ToList();

            var message = tasks.Count == 0
                ? "No se puede cerrar el proceso. El checklist no tiene tareas."
                : "No se puede cerrar el proceso. Existen tareas obligatorias pendientes.";
            return new CloseResult(null, pending, message);
        }

        // Cierra solo si sigue abierto y sin tareas obligatorias incompletas.
        // Evita cierres dobles y carreras con el PATCH.
        var closedAt = DateTime.UtcNow;
        var rows = await db.EmployeeExits
            .Where(e => e.Id == exitId
                && e.Status != EmployeeExitStatuses.Closed
                && !e.Checklist.Tasks.Any(t =>
                    t.IsRequired && t.Status != ChecklistTaskStatus.Completed))
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.Status, EmployeeExitStatuses.Closed)
                .SetProperty(e => e.ClosedAt, closedAt)
                .SetProperty(e => e.ClosedByUserId, userId),
                cancellationToken);

        if (rows == 0)
        {
            throw new EmployeeExitConflictException(
                "El proceso cambió mientras se cerraba. Vuelve a intentarlo.");
        }

        await transaction.CommitAsync(cancellationToken);

        return new CloseResult(
            new CloseExitResponse(exitId, EmployeeExitStatuses.Closed, closedAt, userId),
            null,
            null);
    }
}