using RelayHR.Web.Entities;
using RelayHR.Web.Models;
using RelayHR.Web.Repositories;

namespace RelayHR.Web.Services;

public sealed class EmployeeExitService(
    IEmployeeExitRepository repository,
    IExitChecklistService checklistService) : IEmployeeExitService
{
    public async Task<CreateEmployeeExitResponse> CreateAsync(
        CreateEmployeeExitDTO request,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (request.EffectiveDate < today)
        {
            throw new InvalidEffectiveDateException(
                "La fecha efectiva debe ser hoy o una fecha futura.");
        }

        if (!await repository.EmployeeExistsAsync(request.EmployeeId, cancellationToken))
        {
            throw new EmployeeNotFoundException(request.EmployeeId);
        }

        if (await repository.HasOpenExitAsync(request.EmployeeId, cancellationToken))
        {
            throw new EmployeeExitConflictException(
                "Ya existe una salida activa o en proceso para este empleado.");
        }

        var employeeExit = new EmployeeExit
        {
            EmployeeId = request.EmployeeId,
            EffectiveDate = request.EffectiveDate,
            CreatedAt = DateTime.UtcNow,
            Status = EmployeeExitStatuses.InProgress
        };
        var checklist = await checklistService.GenerateAsync(
            employeeExit,
            cancellationToken);
        employeeExit.Checklist = checklist;

        var checklistId = await repository.CreateWithChecklistAsync(
            employeeExit,
            checklist,
            cancellationToken);

        return new CreateEmployeeExitResponse(employeeExit.Id, checklistId);
    }

    public async Task<ExitChecklistResponseDTO?> GetChecklistAsync(
        int employeeExitId,
        CancellationToken cancellationToken)
    {
        var checklist = await repository.GetChecklistAsync(employeeExitId, cancellationToken);
        if (checklist is null)
        {
            return null;
        }

        var tasks = checklist.Tasks
            .OrderBy(task => task.Department.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(task => task.DueDate)
            .ThenBy(task => task.Id)
            .Select(task => new ChecklistTaskResponseDTO(
                task.Id,
                task.Title,
                task.Department.Name,
                task.ResponsibleUserId,
                task.ResponsibleUser.Name,
                task.DueDate,
                task.Status.ToString()))
            .ToArray();

        return new ExitChecklistResponseDTO(
            checklist.Id,
            checklist.EmployeeExitId,
            tasks);
    }
}
