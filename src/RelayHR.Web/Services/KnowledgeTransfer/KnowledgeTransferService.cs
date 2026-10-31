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
