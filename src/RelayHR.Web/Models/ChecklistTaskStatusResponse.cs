namespace RelayHR.Web.Models
{
    /// <summary>Datos de la tarea después de actualizar su estado.</summary>
    public sealed record ChecklistTaskStatusResponse(
        int Id,
        string Title,
        string Status,
        bool IsRequired,
        DateTime? CompletedAt,
        int? CompletedByUserId,
        string? Notes);
}
