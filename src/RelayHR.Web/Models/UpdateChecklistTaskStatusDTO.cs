using System.ComponentModel.DataAnnotations;

namespace RelayHR.Web.Models;

/// <summary>
/// Cuerpo de PATCH /api/checklist-tasks/{taskId}/status
/// Ejemplo: { "status": "Completed", "notes": "Equipo devuelto y verificado." }
/// </summary>
/// <remarks>
/// No incluye ningún campo de usuario: quién completa la tarea se obtiene
/// del usuario autenticado, nunca de lo que envía el cliente.
/// </remarks>
public class UpdateChecklistTaskStatusDTO
{
    /// <summary>Valores permitidos: Pending, InProgress, Completed.</summary>
    [Required(ErrorMessage = "El estado es obligatorio.")]
    public string Status { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Las notas no pueden superar los 500 caracteres.")]
    public string? Notes { get; set; }
}
