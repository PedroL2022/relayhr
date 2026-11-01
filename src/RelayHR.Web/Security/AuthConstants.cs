namespace RelayHR.Web.Security;

/// <summary>
/// Roles que se esperan en el claim "role" del token JWT.
/// </summary>
public static class Roles
{
    public const string Recruiter = "Recruiter";
    public const string HRAdmin = "HRAdmin";
}

public static class Policies
{
    // Solo reclutadores y administradores de RRHH pueden consultar las vacantes.
    public const string RecruiterAccess = "RecruiterAccess";
}
