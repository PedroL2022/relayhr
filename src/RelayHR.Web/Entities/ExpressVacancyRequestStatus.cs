namespace RelayHR.Web.Entities;

/// <summary>
/// Estados de una solicitud de generación de Vacante Express.
/// </summary>
public enum ExpressVacancyRequestStatus
{
    // Esperando a ser procesada (o a que llegue la hora del próximo reintento).
    Pending = 1,

    // Una instancia de la aplicación la tomó y la está procesando.
    Processing = 2,

    // La vacante se generó (o ya existía) y la solicitud terminó.
    Completed = 3,

    // Se agotaron los reintentos o el error no es recuperable. Requiere revisión manual.
    Failed = 4
}