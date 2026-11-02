namespace RelayHR.Web.Entities;

public enum VacancyStatus
{
    // La vacante se generó sola y nadie la ha revisado.
    PendingReview = 1,

    // Alguien de RRHH o reclutamiento ya la editó, pero todavía no está publicada.
    InReview = 2,

    // Visible para los candidatos.
    Published = 3
}

/// <summary>
/// Vacante generada automáticamente a partir de la salida de un empleado (HU-04).
/// Los datos del puesto son una copia del perfil usado al generarla: si el puesto cambia
/// después, la vacante no se modifica. Los campos sin información quedan vacíos y se
/// muestran como "por completar" hasta que alguien los llene (HU-05).
/// </summary>
public class ExpressVacancy
{
    // Meta del Product Owner: publicar en menos de 48 horas desde que se registra la salida.
    public const int PublishGoalHours = 48;

    public int ExpressVacancyId { get; set; }

    // Salida que originó la vacante (obligatoria y única).
    public int EmployeeExitId { get; set; }
    public EmployeeExit EmployeeExit { get; set; } = null!;

    // Puesto de origen, solo como referencia.
    public int? PositionId { get; set; }
    public Position? Position { get; set; }

    public string? JobTitle { get; set; }

    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }

    public string? JobDescription { get; set; }
    public string? Requirements { get; set; }

    public VacancyStatus Status { get; set; } = VacancyStatus.PendingReview;

    public DateTime CreatedAt { get; set; }

    public DateTime? PublishedAt { get; set; }
    public string? PublishedBy { get; set; }

    /// <summary>
    /// Genera una vacante a partir de una salida, copiando los datos del puesto.
    /// Lo que no tenga información queda vacío (aparecerá como "por completar").
    /// </summary>
    public static ExpressVacancy CreateFromExit(EmployeeExit employeeExit, Position? position, DateTime createdAt)
    {
        ArgumentNullException.ThrowIfNull(employeeExit);

        return new ExpressVacancy
        {
            EmployeeExit = employeeExit,
            PositionId = position?.PositionId,
            Position = position,
            JobTitle = Clean(position?.Title),
            // Si el puesto no tiene departamento se usa el del empleado que salió.
            DepartmentId = position?.DepartmentId ?? employeeExit.Employee?.DepartmentId,
            JobDescription = Clean(position?.Description),
            Requirements = Clean(position?.Requirements),
            Status = VacancyStatus.PendingReview,
            CreatedAt = createdAt
        };
    }

    /// <summary>Nombres (en español) de los campos obligatorios que siguen vacíos.</summary>
    public List<string> GetMissingFields()
    {
        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(JobTitle)) missing.Add("Título del puesto");
        if (DepartmentId is null) missing.Add("Departamento");
        if (string.IsNullOrWhiteSpace(JobDescription)) missing.Add("Descripción");
        if (string.IsNullOrWhiteSpace(Requirements)) missing.Add("Requisitos");

        return missing;
    }

    public bool CanEdit => Status != VacancyStatus.Published;

    /// <summary>Guarda los cambios de la revisión. La vacante pasa a "en revisión".</summary>
    public void Edit(string? jobTitle, int? departmentId, string? jobDescription, string? requirements)
    {
        JobTitle = Clean(jobTitle);
        DepartmentId = departmentId;
        JobDescription = Clean(jobDescription);
        Requirements = Clean(requirements);

        if (Status == VacancyStatus.PendingReview)
        {
            Status = VacancyStatus.InReview;
        }
    }

    /// <summary>
    /// Publica la vacante (HU-05). No se puede publicar si faltan campos obligatorios:
    /// en ese caso devuelve false y la lista de lo que falta.
    /// </summary>
    public bool TryPublish(string publishedBy, DateTime now, out List<string> missingFields)
    {
        missingFields = GetMissingFields();

        if (Status == VacancyStatus.Published || missingFields.Count > 0)
        {
            return false;
        }

        Status = VacancyStatus.Published;
        PublishedAt = now;
        PublishedBy = publishedBy;
        return true;
    }

    /// <summary>Tiempo entre el registro de la salida y la publicación (requiere la salida cargada).</summary>
    public TimeSpan? TimeToPublish =>
        PublishedAt.HasValue && EmployeeExit is not null
            ? PublishedAt.Value - EmployeeExit.CreatedAt
            : null;

    public bool MetPublishGoal =>
        TimeToPublish is { } time && time < TimeSpan.FromHours(PublishGoalHours);

    // Convierte textos vacíos o con solo espacios en null.
    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
