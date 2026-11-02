using RelayHR.Web.Entities;

namespace RelayHR.Web.Helpers;

/// <summary>
/// Textos en español y colores de las etiquetas (badges) para mostrar los estados en las vistas.
/// </summary>
public static class Display
{
    public static string ToText(this ExitStatus status) => status switch
    {
        ExitStatus.InProgress => "En proceso",
        ExitStatus.Closed => "Cerrada",
        _ => status.ToString()
    };

    public static string ToBadge(this ExitStatus status) => status switch
    {
        ExitStatus.InProgress => "bg-warning text-dark",
        ExitStatus.Closed => "bg-success",
        _ => "bg-secondary"
    };

    public static string ToText(this ExitTaskStatus status) => status switch
    {
        ExitTaskStatus.Pending => "Pendiente",
        ExitTaskStatus.InProgress => "En proceso",
        ExitTaskStatus.Completed => "Completada",
        _ => status.ToString()
    };

    public static string ToBadge(this ExitTaskStatus status) => status switch
    {
        ExitTaskStatus.Pending => "bg-secondary",
        ExitTaskStatus.InProgress => "bg-warning text-dark",
        ExitTaskStatus.Completed => "bg-success",
        _ => "bg-secondary"
    };

    public static string ToText(this ExitArea area) => area switch
    {
        ExitArea.Technology => "Tecnología",
        ExitArea.HumanResources => "Recursos Humanos",
        ExitArea.Finance => "Finanzas",
        _ => area.ToString()
    };

    public static string ToText(this VacancyStatus status) => status switch
    {
        VacancyStatus.PendingReview => "Pendiente de revisión",
        VacancyStatus.InReview => "En revisión",
        VacancyStatus.Published => "Publicada",
        _ => status.ToString()
    };

    public static string ToBadge(this VacancyStatus status) => status switch
    {
        VacancyStatus.PendingReview => "bg-secondary",
        VacancyStatus.InReview => "bg-warning text-dark",
        VacancyStatus.Published => "bg-success",
        _ => "bg-secondary"
    };

    public static string ToText(this ServiceRequestStatus status) => status switch
    {
        ServiceRequestStatus.Sent => "Enviada",
        ServiceRequestStatus.Accepted => "Aceptada",
        ServiceRequestStatus.Rejected => "Rechazada",
        ServiceRequestStatus.Confirmed => "Confirmada",
        _ => status.ToString()
    };

    public static string ToBadge(this ServiceRequestStatus status) => status switch
    {
        ServiceRequestStatus.Sent => "bg-secondary",
        ServiceRequestStatus.Accepted => "bg-info text-dark",
        ServiceRequestStatus.Rejected => "bg-danger",
        ServiceRequestStatus.Confirmed => "bg-success",
        _ => "bg-secondary"
    };

    // Las fechas se guardan en UTC; aquí se muestran en la hora local del servidor.
    public static string Local(this DateTime utc) =>
        DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime().ToString("dd/MM/yyyy HH:mm");

    public static string Local(this DateTime? utc) => utc.HasValue ? utc.Value.Local() : "—";

    public static string Short(this DateOnly date) => date.ToString("dd/MM/yyyy");

    public static string Money(this decimal amount) => $"RD$ {amount:N2}";

    public static string AsHours(this TimeSpan time) => $"{time.TotalHours:N1} h";
}
