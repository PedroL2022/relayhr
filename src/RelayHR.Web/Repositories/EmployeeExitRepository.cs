using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RelayHR.Web.Data;
using RelayHR.Web.Entities;
using RelayHR.Web.Services;

namespace RelayHR.Web.Repositories;

public sealed class EmployeeExitRepository(RelayHrDbContext dbContext) : IEmployeeExitRepository
{
    public Task<bool> EmployeeExistsAsync(int employeeId, CancellationToken cancellationToken) =>
        dbContext.Employees.AnyAsync(employee => employee.Id == employeeId, cancellationToken);

    public Task<bool> HasOpenExitAsync(int employeeId, CancellationToken cancellationToken) =>
        dbContext.EmployeeExits.AnyAsync(
            employee => employee.EmployeeId == employeeId &&
                (employee.Status == EmployeeExitStatuses.Pending ||
                 employee.Status == EmployeeExitStatuses.InProgress),
            cancellationToken);

    public async Task<IReadOnlyList<Department>> GetDepartmentsAsync(
        IReadOnlyCollection<string> departmentNames,
        CancellationToken cancellationToken) =>
        await dbContext.Departments
            .Where(department => departmentNames.Contains(department.Name))
            .ToListAsync(cancellationToken);

    public async Task<HashSet<int>> GetExistingUserIdsAsync(
        IReadOnlyCollection<int> userIds,
        CancellationToken cancellationToken) =>
        await dbContext.Users
            .Where(user => userIds.Contains(user.Id))
            .Select(user => user.Id)
            .ToHashSetAsync(cancellationToken);

    public async Task<int> CreateWithChecklistAsync(
        EmployeeExit employeeExit,
        ExitChecklist checklist,
        CancellationToken cancellationToken)
    {
        // La salida y todas sus tareas se confirman o revierten juntas.
        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            employeeExit.Checklist = checklist;
            checklist.EmployeeExit = employeeExit;
            dbContext.EmployeeExits.Add(employeeExit);

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return checklist.Id;
        }
        catch (DbUpdateException exception) when (IsOpenExitUniqueConstraintViolation(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new EmployeeExitConflictException(
                "Ya existe una salida activa o en proceso para este empleado.",
                exception);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<ExitChecklist> CreateChecklistIfMissingAsync(
        int employeeExitId,
        ExitChecklist checklist,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var existingChecklist = await dbContext.ExitChecklists
            .AsNoTracking()
            .Include(existing => existing.Tasks)
            .SingleOrDefaultAsync(
                existing => existing.EmployeeExitId == employeeExitId,
                cancellationToken);
        if (existingChecklist is not null)
        {
            await transaction.CommitAsync(cancellationToken);
            return existingChecklist;
        }

        checklist.EmployeeExitId = employeeExitId;
        dbContext.ExitChecklists.Add(checklist);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return checklist;
        }
        catch (DbUpdateException exception) when (
            IsUniqueConstraintViolation(exception, "IX_ExitChecklists_EmployeeExitId"))
        {
            await transaction.RollbackAsync(cancellationToken);
            DetachChecklist(checklist);

            return await dbContext.ExitChecklists
                .AsNoTracking()
                .Include(existing => existing.Tasks)
                .SingleAsync(
                    existing => existing.EmployeeExitId == employeeExitId,
                    cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            DetachChecklist(checklist);
            throw;
        }
    }

    public Task<ExitChecklist?> GetChecklistAsync(
        int employeeExitId,
        CancellationToken cancellationToken) =>
        dbContext.ExitChecklists
            .AsNoTracking()
            .Include(checklist => checklist.EmployeeExit)
            .Include(checklist => checklist.Tasks)
                .ThenInclude(task => task.Department)
            .Include(checklist => checklist.Tasks)
                .ThenInclude(task => task.ResponsibleUser)
            .SingleOrDefaultAsync(
                checklist => checklist.EmployeeExitId == employeeExitId,
                cancellationToken);

    private static bool IsOpenExitUniqueConstraintViolation(DbUpdateException exception) =>
        IsUniqueConstraintViolation(exception, "IX_EmployeeExits_EmployeeId");

    private static bool IsUniqueConstraintViolation(
        DbUpdateException exception,
        string indexName)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is SqlException sqlException &&
                sqlException.Errors.Cast<SqlError>().Any(error =>
                    error.Number is 2601 or 2627 &&
                    sqlException.Message.Contains(
                        indexName,
                        StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        return false;
    }

    private void DetachChecklist(ExitChecklist checklist)
    {
        foreach (var task in checklist.Tasks)
        {
            dbContext.Entry(task).State = EntityState.Detached;
        }

        dbContext.Entry(checklist).State = EntityState.Detached;
    }
}
