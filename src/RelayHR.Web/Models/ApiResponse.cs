using System.ComponentModel.DataAnnotations;

namespace RelayHR.Web.Models;

/// <summary>Envoltura uniforme para respuestas exitosas de la API.</summary>
public sealed record ApiResponse<T>(bool Success, string Message, T Data);

/// <summary>Identificadores generados al registrar una salida.</summary>
public sealed record CreateEmployeeExitResponse(int EmployeeExitId, int ChecklistId);

/// <summary>Credenciales para iniciar sesión con una cuenta de Identity.</summary>
public sealed record LoginRequest
{
    [Required, EmailAddress]
    public required string Email { get; init; }

    [Required]
    public required string Password { get; init; }
}
