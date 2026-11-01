namespace RelayHR.Web.Entities;

/// <summary>
/// Solicitud persistente para generar la Vacante Express de una salida.
/// Se guarda en la misma transacción que la salida, así que si la aplicación se
/// reinicia la solicitud no se pierde y el proceso en segundo plano la retoma.
/// </summary>
public class ExpressVacancyRequest
{
    public int ExpressVacancyRequestId { get; set; }

    // Salida que originó la solicitud. Es única: una salida solo tiene una solicitud.
    public int EmployeeExitId { get; set; }
    public EmployeeExit EmployeeExit { get; set; } = null!;

    public ExpressVacancyRequestStatus Status { get; set; } = ExpressVacancyRequestStatus.Pending;

    // Intentos realizados (incluye el que está en curso).
    public int Attempts { get; set; }

    // Momento a partir del cual se puede procesar o reintentar.
    public DateTime NextAttemptAt { get; set; }

    // Mientras la solicitud está en Processing, hasta cuándo es válido el bloqueo.
    // Si la aplicación se cae a mitad del proceso, al vencer el bloqueo otra ejecución la retoma.
    public DateTime? LockedUntil { get; set; }

    public DateTime? LastAttemptAt { get; set; }

    // Mensaje del último error, para diagnosticar sin buscar en los logs.
    public string? LastError { get; set; }

    // Fecha en que se registró la salida (se usa para medir el límite de 5 minutos).
    public DateTime CreatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    // Tiempo que tomó generar la vacante en el intento exitoso.
    public long? ProcessingMilliseconds { get; set; }
}