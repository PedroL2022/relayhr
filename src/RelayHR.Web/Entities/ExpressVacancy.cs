namespace RelayHR.Web.Entities;

/// <summary>
/// Vacante generada automáticamente a partir de la salida de un empleado.
/// Los datos del puesto son una copia del perfil usado al generarla, así que si el
/// puesto original cambia después, la vacante no se modifica.
/// </summary>
public class ExpressVacancy
{
    // Nombres de los campos del perfil que pueden quedar pendientes de completar.
    public static class Fields
    {
        public const string JobTitle = nameof(JobTitle);
        public const string DepartmentId = nameof(DepartmentId);
        public const string JobDescription = nameof(JobDescription);
        public const string Requirements = nameof(Requirements);
    }

    // Marcador explícito para los campos del perfil que no tienen información.
    // No es un dato inventado: indica que alguien debe completarlo en la revisión.
    public const string ToComplete = "ToComplete";

    public int ExpressVacancyId { get; set; }

    // Salida que originó la vacante (obligatoria y única).
    public int EmployeeExitId { get; set; }
    public EmployeeExit EmployeeExit { get; set; } = null!;

    // Puesto de origen, solo como referencia. Es opcional porque el puesto puede no existir.
    public int? PositionId { get; set; }
    public Position? Position { get; set; }

    // Copia de los datos del puesto. Son opcionales: que falten no impide crear la vacante.
    public string? JobTitle { get; set; }

    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }

    public string? JobDescription { get; set; }
    public string? Requirements { get; set; }

    public ExpressVacancyStatus Status { get; set; } = ExpressVacancyStatus.PendingReview;

    // Campos del perfil que no tenían información al generar la vacante.
    public List<string> MissingFields { get; set; } = new();

    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Genera una vacante a partir de una salida, copiando los datos del puesto
    /// y registrando como pendientes los campos que no tengan información.
    /// </summary>
    /// <param name="employeeExit">Salida que origina la vacante.</param>
    /// <param name="position">Puesto del empleado, si existe.</param>
    /// <param name="createdAt">Fecha de generación (UTC).</param>
    public static ExpressVacancy CreateFromExit(EmployeeExit employeeExit, Position? position, DateTime createdAt)
    {
        ArgumentNullException.ThrowIfNull(employeeExit);

        var vacancy = new ExpressVacancy
        {
            EmployeeExitId = employeeExit.EmployeeExitId,
            EmployeeExit = employeeExit,
            PositionId = position?.PositionId,
            Position = position,
            JobTitle = Clean(position?.Title),
            // Si el puesto no tiene departamento se usa el del empleado que salió.
            DepartmentId = position?.DepartmentId ?? employeeExit.Employee?.DepartmentId,
            JobDescription = Clean(position?.Description),
            Requirements = Clean(position?.Requirements),
            Status = ExpressVacancyStatus.PendingReview,
            CreatedAt = createdAt
        };

        vacancy.MissingFields = vacancy.CalculateMissingFields();
        vacancy.MarkMissingFieldsAsToComplete();
        return vacancy;
    }

    /// <summary>
    /// Devuelve la lista de campos del perfil que siguen sin información.
    /// Un texto con el marcador ToComplete cuenta como pendiente.
    /// Sirve también para recalcular la lista cuando RRHH completa datos.
    /// </summary>
    public List<string> CalculateMissingFields()
    {
        var missing = new List<string>();

        if (IsEmpty(JobTitle)) missing.Add(Fields.JobTitle);
        if (DepartmentId is null) missing.Add(Fields.DepartmentId);
        if (IsEmpty(JobDescription)) missing.Add(Fields.JobDescription);
        if (IsEmpty(Requirements)) missing.Add(Fields.Requirements);

        return missing;
    }

    /// <summary>
    /// Escribe el marcador ToComplete en los campos de texto que están pendientes.
    /// El departamento es numérico, así que se queda en null y se identifica en MissingFields.
    /// </summary>
    public void MarkMissingFieldsAsToComplete()
    {
        if (MissingFields.Contains(Fields.JobTitle)) JobTitle = ToComplete;
        if (MissingFields.Contains(Fields.JobDescription)) JobDescription = ToComplete;
        if (MissingFields.Contains(Fields.Requirements)) Requirements = ToComplete;
    }

    private static bool IsEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) || value == ToComplete;

    // Convierte textos vacíos o con solo espacios en null para tratarlos como "sin información".
    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
