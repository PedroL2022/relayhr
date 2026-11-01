using RelayHR.Web.Entities;

namespace RelayHR.Web.Dtos;

/// <summary>
/// Datos completos de una Vacante Express para el reclutador.
/// </summary>
public class ExpressVacancyResponseDTO
{
    public int ExpressVacancyId { get; set; }

    // Salida que originó la vacante.
    public int EmployeeExitId { get; set; }

    public int? DepartmentId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedByUserId { get; set; }
    public DateTime? PublishedAt { get; set; }
    public int? PublishedByUserId { get; set; }
    public DateTime? EffectiveExitAt { get; set; }
    public double? HoursToPublication { get; set; }
    public bool? Meets48HourTarget { get; set; }
    public bool PublishedBeforeEffectiveExit { get; set; }

    public int? PositionId { get; set; }
    public string? JobTitle { get; set; }

    // Nombre del departamento, o "ToComplete" si todavía no tiene.
    public string? Department { get; set; }

    public string? JobDescription { get; set; }
    public string? Requirements { get; set; }

    // Nombres de los campos pendientes de completar. Esos campos se muestran con el valor "ToComplete".
    public List<string> MissingFields { get; set; } = new();

    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    private static DateTime? Utc(DateTime? value) => value.HasValue
        ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : null;
    private static DateTime? ExitAt(ExpressVacancy v) => v.EmployeeExit is null ? null
        : v.EmployeeExit.EffectiveDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
    private static double? Hours(ExpressVacancy v) => v.PublishedAt.HasValue && ExitAt(v) is DateTime exitAt
        && v.PublishedAt.Value >= exitAt ? (v.PublishedAt.Value - exitAt).TotalHours : null;

    // La entidad debe venir con el departamento cargado (Include).
    public static ExpressVacancyResponseDTO FromEntity(ExpressVacancy vacancy) => new()
    {
        ExpressVacancyId = vacancy.ExpressVacancyId,
        EmployeeExitId = vacancy.EmployeeExitId,
        PositionId = vacancy.PositionId,
        DepartmentId = vacancy.DepartmentId,
        UpdatedAt = Utc(vacancy.UpdatedAt), UpdatedByUserId = vacancy.UpdatedByUserId,
        PublishedAt = Utc(vacancy.PublishedAt), PublishedByUserId = vacancy.PublishedByUserId,
        EffectiveExitAt = ExitAt(vacancy),
        HoursToPublication = Hours(vacancy),
        Meets48HourTarget = Hours(vacancy) is double hours ? hours < 48 : null,
        PublishedBeforeEffectiveExit = vacancy.PublishedAt.HasValue && ExitAt(vacancy) is DateTime exitAt
            && vacancy.PublishedAt.Value < exitAt,
        JobTitle = vacancy.JobTitle,
        Department = vacancy.Department?.Name ?? ExpressVacancy.ToComplete,
        JobDescription = vacancy.JobDescription,
        Requirements = vacancy.Requirements,
        MissingFields = vacancy.MissingFields.ToList(),
        Status = vacancy.Status.ToString(),
        // SQL Server devuelve la fecha sin zona horaria; se indica que es UTC.
        CreatedAt = DateTime.SpecifyKind(vacancy.CreatedAt, DateTimeKind.Utc)
    };
}
