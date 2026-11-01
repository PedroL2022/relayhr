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

    // La entidad debe venir con el departamento cargado (Include).
    public static ExpressVacancyResponseDTO FromEntity(ExpressVacancy vacancy) => new()
    {
        ExpressVacancyId = vacancy.ExpressVacancyId,
        EmployeeExitId = vacancy.EmployeeExitId,
        PositionId = vacancy.PositionId,
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
