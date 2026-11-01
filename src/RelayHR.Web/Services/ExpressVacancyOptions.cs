namespace RelayHR.Web.Services;

/// <summary>
/// Parámetros de la generación automática. Se leen de la sección "ExpressVacancy" de appsettings.json.
/// </summary>
public class ExpressVacancyOptions
{
    public const string SectionName = "ExpressVacancy";

    // Cada cuántos segundos el proceso busca solicitudes pendientes.
    public int PollingIntervalSeconds { get; set; } = 10;

    // Máximo de solicitudes que se toman en cada ciclo.
    public int BatchSize { get; set; } = 20;

    // Intentos totales antes de marcar la solicitud como Failed.
    public int MaxAttempts { get; set; } = 5;

    // Espera antes del primer reintento. Se duplica en cada intento siguiente.
    public int RetryBaseDelaySeconds { get; set; } = 15;

    // Tope de espera entre reintentos.
    public int MaxRetryDelaySeconds { get; set; } = 120;

    // Tiempo que una solicitud puede estar bloqueada. Si la aplicación se cae, pasado
    // este tiempo otra ejecución la retoma.
    public int LeaseSeconds { get; set; } = 120;

    // Límite acordado: la vacante debe estar disponible dentro de este tiempo desde el registro de la salida.
    public int MaxAvailabilityMinutes { get; set; } = 5;

    /// <summary>
    /// Calcula la espera antes del siguiente reintento (15s, 30s, 60s, 120s, 120s...).
    /// Con los valores por defecto los cuatro reintentos suman 225 segundos, dentro de los 5 minutos.
    /// </summary>
    public TimeSpan GetRetryDelay(int attemptsDone)
    {
        var exponent = Math.Clamp(attemptsDone - 1, 0, 20);
        var seconds = RetryBaseDelaySeconds * Math.Pow(2, exponent);
        return TimeSpan.FromSeconds(Math.Min(seconds, MaxRetryDelaySeconds));
    }
}