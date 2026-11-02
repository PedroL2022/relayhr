using System.Security.Claims;

namespace RelayHR.Web.Security;

/// <summary>
/// Roles de la plataforma. Cada cuenta tiene exactamente uno.
/// </summary>
public static class Roles
{
    // Administrador de Recursos Humanos: salidas, traspaso, vacantes, directorio y solicitudes.
    public const string HRAdmin = "HRAdmin";

    // Reclutador: bandeja de Vacantes Express, directorio y solicitudes.
    public const string Recruiter = "Recruiter";

    // Trabajador independiente: administra su perfil y responde solicitudes.
    public const string Freelancer = "Freelancer";

    // Valor para [Authorize(Roles = ...)]: la coma significa "cualquiera de estos".
    public const string Company = HRAdmin + "," + Recruiter;

    public static readonly IReadOnlyList<string> All = new[] { HRAdmin, Recruiter, Freelancer };

    public static bool IsCompanyRole(string? role) => role is HRAdmin or Recruiter;

    public static string DisplayName(string role) => role switch
    {
        HRAdmin => "Administrador de RRHH",
        Recruiter => "Reclutador",
        Freelancer => "Trabajador independiente",
        _ => role
    };
}

public static class ClaimsPrincipalExtensions
{
    /// <summary>Id del usuario que inició sesión (0 si no hay sesión).</summary>
    public static int GetUserId(this ClaimsPrincipal user) =>
        int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    /// <summary>Nombre completo del usuario, que se guarda como responsable en los registros.</summary>
    public static string GetDisplayName(this ClaimsPrincipal user) =>
        user.Identity?.Name ?? "Usuario";
}
