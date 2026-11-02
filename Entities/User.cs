namespace RelayHR.Web.Entities;

/// <summary>
/// Cuenta de acceso a la plataforma. El campo Role decide qué puede hacer el usuario
/// (ver Security/Roles.cs).
/// </summary>
public class User
{
    public int UserId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    // Solo para usuarios de empresa (HRAdmin y Recruiter).
    public string? CompanyName { get; set; }

    public DateTime CreatedAt { get; set; }
}
