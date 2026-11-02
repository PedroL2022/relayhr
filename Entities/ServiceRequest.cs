namespace RelayHR.Web.Entities;

public enum ServiceRequestStatus
{
    // La empresa la envió y el profesional aún no responde.
    Sent = 1,

    // El profesional aceptó.
    Accepted = 2,

    // El profesional rechazó.
    Rejected = 3,

    // La empresa confirmó el servicio ya aceptado.
    Confirmed = 4
}

/// <summary>
/// Solicitud de servicio de una empresa a un trabajador independiente (HU-07).
/// La plataforma solo registra la solicitud y su confirmación: no procesa pagos ni genera contratos.
/// </summary>
public class ServiceRequest
{
    public int ServiceRequestId { get; set; }

    public int FreelancerProfileId { get; set; }
    public FreelancerProfile FreelancerProfile { get; set; } = null!;

    public int RequestedByUserId { get; set; }
    public User RequestedBy { get; set; } = null!;

    // Datos de la empresa copiados al crear la solicitud.
    public string CompanyName { get; set; } = string.Empty;

    public ServiceRequestStatus Status { get; set; } = ServiceRequestStatus.Sent;

    public DateTime CreatedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }

    /// <summary>El profesional acepta o rechaza. Solo se puede responder una solicitud enviada.</summary>
    public bool Respond(bool accept, DateTime now)
    {
        if (Status != ServiceRequestStatus.Sent)
        {
            return false;
        }

        Status = accept ? ServiceRequestStatus.Accepted : ServiceRequestStatus.Rejected;
        RespondedAt = now;
        return true;
    }

    /// <summary>La empresa confirma el servicio. Solo se puede confirmar una solicitud aceptada.</summary>
    public bool Confirm(DateTime now)
    {
        if (Status != ServiceRequestStatus.Accepted)
        {
            return false;
        }

        Status = ServiceRequestStatus.Confirmed;
        ConfirmedAt = now;
        return true;
    }

    // Una solicitud "activa" impide enviar otra igual al mismo profesional.
    public bool IsActive => Status is ServiceRequestStatus.Sent or ServiceRequestStatus.Accepted;
}
