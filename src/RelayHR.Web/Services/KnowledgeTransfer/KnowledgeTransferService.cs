using Microsoft.EntityFrameworkCore;
using RelayHR.Web.Data.KnowledgeTransfer;
using RelayHR.Web.DTOs.KnowledgeTransfer;
using RelayHR.Web.Entities.KnowledgeTransfer;

namespace RelayHR.Web.Services.KnowledgeTransfer;

public class KnowledgeTransferService : IKnowledgeTransferService
{
    private const int MaxNameLength = 200;
    private const int MaxDescriptionLength = 2000;
    private const string ExitNotFoundMessage = "No existe una salida registrada con el identificador indicado.";

    private readonly IKnowledgeTransferDbContext _db;

    public KnowledgeTransferService(IKnowledgeTransferDbContext db)
    {
        _db = db;
    }

    public async Task<OperationResult<KnowledgeTransferProjectResponseDTO>> CreateProjectAsync(
        int employeeExitId, CreateKnowledgeTransferProjectDTO dto, CancellationToken cancellationToken = default)
    {
        var exit = await _db.EmployeeExits.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == employeeExitId, cancellationToken);
        if (exit is null)
            return OperationResult<KnowledgeTransferProjectResponseDTO>.NotFound(ExitNotFoundMessage);

        var errors = new List<string>();
        ValidateText(dto.Name, "El nombre", MaxNameLength, errors);
        ValidateText(dto.Description, "La descripción", MaxDescriptionLength, errors);

        // En proyectos el estado es opcional: por defecto, Pending.
        var status = KnowledgeTransferStatus.Pending;
        if (!string.IsNullOrWhiteSpace(dto.Status) && !TryParseStatus(dto.Status, out status))
            errors.Add(InvalidStatusMessage);

        if (errors.Count > 0)
            return OperationResult<KnowledgeTransferProjectResponseDTO>.Invalid(errors);

        var project = new KnowledgeTransferProject
        {
            EmployeeExitId = exit.Id,
            DepartingEmployeeId = exit.EmployeeId,
            Name = dto.Name!.Trim(),
            Description = dto.Description!.Trim(),
            Status = status,
            CreatedAt = DateTime.UtcNow
        };

        _db.KnowledgeTransferProjects.Add(project);
        await _db.SaveChangesAsync(cancellationToken);

        return OperationResult<KnowledgeTransferProjectResponseDTO>.Created(
            new KnowledgeTransferProjectResponseDTO(
                project.Id, project.EmployeeExitId, project.DepartingEmployeeId,
                project.Name, project.Description, project.Status.ToString(), project.CreatedAt),
            "Proyecto registrado correctamente.");
    }

    public async Task<OperationResult<KnowledgeTransferTaskResponseDTO>> CreateTaskAsync(
        int employeeExitId, CreateKnowledgeTransferTaskDTO dto, CancellationToken cancellationToken = default)
    {
        var exit = await _db.EmployeeExits.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == employeeExitId, cancellationToken);
        if (exit is null)
            return OperationResult<KnowledgeTransferTaskResponseDTO>.NotFound(ExitNotFoundMessage);

        var errors = new List<string>();
        ValidateText(dto.Title, "El título", MaxNameLength, errors);
        ValidateText(dto.Description, "La descripción", MaxDescriptionLength, errors);

        // En tareas el estado es obligatorio.
        KnowledgeTransferStatus status = default;
        if (string.IsNullOrWhiteSpace(dto.Status))
            errors.Add("El estado es obligatorio.");
        else if (!TryParseStatus(dto.Status, out status))
            errors.Add(InvalidStatusMessage);

        // Si se indica un proyecto, debe pertenecer a la misma salida.
        if (dto.ProjectId.HasValue)
        {
            var projectBelongsToExit = await _db.KnowledgeTransferProjects.AsNoTracking()
                .AnyAsync(p => p.Id == dto.ProjectId.Value && p.EmployeeExitId == employeeExitId, cancellationToken);
            if (!projectBelongsToExit)
                errors.Add("El proyecto indicado no existe o no pertenece a esta salida.");
        }

        if (errors.Count > 0)
            return OperationResult<KnowledgeTransferTaskResponseDTO>.Invalid(errors);

        var task = new KnowledgeTransferTask
        {
            EmployeeExitId = exit.Id,
            ProjectId = dto.ProjectId,
            DepartingEmployeeId = exit.EmployeeId,
            Title = dto.Title!.Trim(),
            Description = dto.Description!.Trim(),
            Status = status,
            CreatedAt = DateTime.UtcNow
        };

        _db.KnowledgeTransferTasks.Add(task);
        await _db.SaveChangesAsync(cancellationToken);

        return OperationResult<KnowledgeTransferTaskResponseDTO>.Created(
            new KnowledgeTransferTaskResponseDTO(
                task.Id, task.EmployeeExitId, task.ProjectId, task.DepartingEmployeeId,
                task.Title, task.Description, task.Status.ToString(), task.CreatedAt),
            "Tarea registrada correctamente.");
    }

    public async Task<OperationResult<AssignmentResponseDTO>> AssignTemporaryResponsibleAsync(
        int taskId, int? callerUserId, AssignTemporaryResponsibleDTO dto, CancellationToken cancellationToken = default)
    {
        // Solo un usuario identificado puede asignar. Los roles permitidos se definen al unir con la HU-08.
        if (callerUserId is null)
            return OperationResult<AssignmentResponseDTO>.Unauthorized("Debe iniciar sesión para asignar responsables.");

        var task = await _db.KnowledgeTransferTasks
            .FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);
        if (task is null)
            return OperationResult<AssignmentResponseDTO>.NotFound("No existe una tarea con el identificador indicado.");

        if (dto.TemporaryResponsibleUserId is null)
            return OperationResult<AssignmentResponseDTO>.Invalid(
                new List<string> { "El responsable temporal es obligatorio." });

        var user = await _db.AppUsers.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == dto.TemporaryResponsibleUserId.Value, cancellationToken);
        if (user is null)
            return OperationResult<AssignmentResponseDTO>.Invalid(
                new List<string> { "El usuario indicado no existe." });
        if (!user.IsActive)
            return OperationResult<AssignmentResponseDTO>.Invalid(
                new List<string> { "El usuario indicado no está activo." });

        // Una sola columna garantiza un único responsable; reasignar reemplaza al anterior.
        task.TemporaryResponsibleUserId = user.Id;
        task.AssignedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return OperationResult<AssignmentResponseDTO>.Ok(
            new AssignmentResponseDTO(task.Id, user.Id, task.AssignedAt.Value),
            "Responsable temporal asignado correctamente.");
    }

    public async Task<OperationResult<IReadOnlyList<MyTaskDTO>>> GetMyTasksAsync(
        int? userId, CancellationToken cancellationToken = default)
    {
        if (userId is null)
            return OperationResult<IReadOnlyList<MyTaskDTO>>.Unauthorized("Debe iniciar sesión para consultar sus tareas.");

        var tasks = await _db.KnowledgeTransferTasks.AsNoTracking()
            .Where(t => t.TemporaryResponsibleUserId == userId.Value)
            .OrderByDescending(t => t.AssignedAt)
            .ToListAsync(cancellationToken);

        IReadOnlyList<MyTaskDTO> result = tasks
            .Select(t => new MyTaskDTO(
                t.Id, t.EmployeeExitId, t.ProjectId, t.Title, t.Description,
                t.Status.ToString(), t.AssignedAt))
            .ToList();

        return OperationResult<IReadOnlyList<MyTaskDTO>>.Ok(result, "Tareas obtenidas correctamente.");
    }

    private const string InvalidStatusMessage =
        "El estado no es válido. Valores permitidos: Pending, InProgress, Completed.";

    private static void ValidateText(string? value, string fieldName, int maxLength, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add($"{fieldName} es obligatorio.");
        else if (value.Trim().Length > maxLength)
            errors.Add($"{fieldName} no puede superar los {maxLength} caracteres.");
    }

    // Acepta solo los nombres del enum (sin importar mayúsculas), no números sueltos como "7".
    private static bool TryParseStatus(string value, out KnowledgeTransferStatus status)
    {
        var trimmed = value.Trim();
        if (int.TryParse(trimmed, out _))
        {
            status = default;
            return false;
        }

        return Enum.TryParse(trimmed, ignoreCase: true, out status) && Enum.IsDefined(status);
    }
}
