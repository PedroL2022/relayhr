using RelayHR.Web.Entities;
using RelayHR.Web.Repository;

namespace RelayHR.Web.Services;

/// <summary>
/// Proceso de salida de un empleado: registro con checklist y vacante (HU-01 y HU-04),
/// control de cierre (HU-02) y traspaso de conocimiento (HU-03).
/// </summary>
public class ExitService
{
    private readonly IEmployeeRepository _employees;
    private readonly IExitRepository _exits;
    private readonly IExitTaskRepository _tasks;
    private readonly IKnowledgeItemRepository _items;
    private readonly IVacancyRepository _vacancies;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _time;

    public ExitService(
        IEmployeeRepository employees,
        IExitRepository exits,
        IExitTaskRepository tasks,
        IKnowledgeItemRepository items,
        IVacancyRepository vacancies,
        IUnitOfWork unitOfWork,
        TimeProvider time)
    {
        _employees = employees;
        _exits = exits;
        _tasks = tasks;
        _items = items;
        _vacancies = vacancies;
        _unitOfWork = unitOfWork;
        _time = time;
    }

    /// <summary>
    /// Registra la salida de un empleado. En un solo guardado (una sola transacción) crea:
    /// la salida, su checklist por departamento y la Vacante Express de reemplazo.
    /// Como todo ocurre en la misma operación, el checklist y la vacante están disponibles
    /// de inmediato (muy por debajo de los 10 segundos y 5 minutos de los criterios de aceptación).
    /// </summary>
    public async Task<Result<int>> RegisterAsync(
        int employeeId, DateOnly exitDate, string? reason, string registeredBy,
        CancellationToken cancellationToken = default)
    {
        var employee = await _employees.GetWithPositionAsync(employeeId, cancellationToken);

        if (employee is null)
        {
            return Result<int>.Fail("El empleado seleccionado no existe.");
        }

        if (!employee.IsActive)
        {
            return Result<int>.Fail("Este empleado ya tiene una salida registrada.");
        }

        var now = _time.GetUtcNow().UtcDateTime;

        var exit = new EmployeeExit
        {
            Employee = employee,
            ExitDate = exitDate,
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            Status = ExitStatus.InProgress,
            CreatedAt = now,
            RegisteredBy = registeredBy
        };

        exit.GenerateChecklist();
        employee.IsActive = false;

        _exits.Add(exit);
        _vacancies.Add(ExpressVacancy.CreateFromExit(exit, employee.Position, now));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<int>.Ok(exit.EmployeeExitId);
    }

    /// <summary>Cambia el estado de una tarea del checklist (por ejemplo, confirmar la revocación de accesos).</summary>
    public async Task<Result> SetTaskStatusAsync(
        int taskId, ExitTaskStatus status, string userName,
        CancellationToken cancellationToken = default)
    {
        var task = await _tasks.GetWithExitAsync(taskId, cancellationToken);

        if (task is null)
        {
            return Result.Fail("La tarea no existe.");
        }

        if (task.EmployeeExit.Status == ExitStatus.Closed)
        {
            return Result.Fail("La salida ya está cerrada; sus tareas no se pueden modificar.");
        }

        task.SetStatus(status, userName, _time.GetUtcNow().UtcDateTime);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    /// <summary>Cambia el responsable y la fecha límite de una tarea.</summary>
    public async Task<Result> UpdateTaskAsync(
        int taskId, string responsibleName, DateOnly dueDate,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(responsibleName))
        {
            return Result.Fail("Indica el responsable de la tarea.");
        }

        var task = await _tasks.GetWithExitAsync(taskId, cancellationToken);

        if (task is null)
        {
            return Result.Fail("La tarea no existe.");
        }

        if (task.EmployeeExit.Status == ExitStatus.Closed)
        {
            return Result.Fail("La salida ya está cerrada; sus tareas no se pueden modificar.");
        }

        task.ResponsibleName = responsibleName.Trim();
        task.DueDate = dueDate;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    /// <summary>
    /// Cierra la salida. Se bloquea mientras existan tareas obligatorias pendientes y el mensaje
    /// de error lista cuáles son (HU-02).
    /// </summary>
    public async Task<Result> CloseAsync(int exitId, string closedBy, CancellationToken cancellationToken = default)
    {
        var exit = await _exits.GetWithTasksForUpdateAsync(exitId, cancellationToken);

        if (exit is null)
        {
            return Result.Fail("La salida no existe.");
        }

        if (exit.Status == ExitStatus.Closed)
        {
            return Result.Fail("La salida ya estaba cerrada.");
        }

        if (!exit.TryClose(closedBy, _time.GetUtcNow().UtcDateTime, out var pending))
        {
            return Result.Fail(
                $"No se puede cerrar la salida: faltan {pending.Count} tarea(s) obligatoria(s): " +
                string.Join("; ", pending.Select(t => $"{t.Title} ({t.ResponsibleName})")) + ".");
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    /// <summary>Documenta un proyecto o tarea pendiente del empleado que sale (HU-03).</summary>
    public async Task<Result<int>> AddKnowledgeItemAsync(
        int exitId, string title, string description, string currentStatus, int? assignedToEmployeeId,
        CancellationToken cancellationToken = default)
    {
        var exit = await _exits.GetByIdAsync(exitId, cancellationToken);

        if (exit is null)
        {
            return Result<int>.Fail("La salida no existe.");
        }

        if (exit.Status == ExitStatus.Closed)
        {
            return Result<int>.Fail("La salida ya está cerrada; no se puede agregar más información.");
        }

        var assigneeError = await ValidateAssigneeAsync(exit, assignedToEmployeeId, cancellationToken);
        if (assigneeError is not null)
        {
            return Result<int>.Fail(assigneeError);
        }

        var item = new KnowledgeItem
        {
            EmployeeExitId = exitId,
            Title = title.Trim(),
            Description = description.Trim(),
            CurrentStatus = currentStatus.Trim(),
            AssignedToEmployeeId = assignedToEmployeeId,
            CreatedAt = _time.GetUtcNow().UtcDateTime
        };

        _items.Add(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<int>.Ok(item.KnowledgeItemId);
    }

    /// <summary>Asigna (o quita, con null) el responsable temporal de un elemento documentado.</summary>
    public async Task<Result> AssignKnowledgeItemAsync(
        int itemId, int? employeeId, CancellationToken cancellationToken = default)
    {
        var item = await _items.GetWithExitAsync(itemId, cancellationToken);

        if (item is null)
        {
            return Result.Fail("El elemento no existe.");
        }

        if (item.EmployeeExit.Status == ExitStatus.Closed)
        {
            return Result.Fail("La salida ya está cerrada; no se puede cambiar el responsable.");
        }

        var assigneeError = await ValidateAssigneeAsync(item.EmployeeExit, employeeId, cancellationToken);
        if (assigneeError is not null)
        {
            return Result.Fail(assigneeError);
        }

        item.AssignedToEmployeeId = employeeId;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    // El responsable temporal debe ser un empleado activo distinto del que se va.
    private async Task<string?> ValidateAssigneeAsync(
        EmployeeExit exit, int? employeeId, CancellationToken cancellationToken)
    {
        if (employeeId is null)
        {
            return null;
        }

        if (employeeId == exit.EmployeeId)
        {
            return "El responsable temporal no puede ser el empleado que se va.";
        }

        var isActive = await _employees.IsActiveAsync(employeeId.Value, cancellationToken);

        return isActive ? null : "El responsable temporal debe ser un empleado activo.";
    }
}
