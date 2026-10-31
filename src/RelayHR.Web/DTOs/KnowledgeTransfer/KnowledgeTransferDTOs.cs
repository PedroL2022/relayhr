namespace RelayHR.Web.DTOs.KnowledgeTransfer;

// Los campos son nullable a propósito: la validación se hace en el servicio
// para devolver siempre el mismo formato de error.
public class CreateKnowledgeTransferProjectDTO
{
    public string? Name { get; set; }
    public string? Description { get; set; }

    /// <summary>Pending, InProgress o Completed. Si no se envía, se usa Pending.</summary>
    public string? Status { get; set; }
}

public class CreateKnowledgeTransferTaskDTO
{
    /// <summary>Opcional. Si es null, la tarea se registra de forma independiente.</summary>
    public int? ProjectId { get; set; }

    public string? Title { get; set; }
    public string? Description { get; set; }

    /// <summary>Pending, InProgress o Completed. Obligatorio.</summary>
    public string? Status { get; set; }
}

public record KnowledgeTransferProjectResponseDTO(
    int Id,
    int EmployeeExitId,
    int DepartingEmployeeId,
    string Name,
    string Description,
    string Status,
    DateTime CreatedAt);

public record KnowledgeTransferTaskResponseDTO(
    int Id,
    int EmployeeExitId,
    int? ProjectId,
    int DepartingEmployeeId,
    string Title,
    string Description,
    string Status,
    DateTime CreatedAt);

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public IReadOnlyList<string> Errors { get; set; } = Array.Empty<string>();
}
