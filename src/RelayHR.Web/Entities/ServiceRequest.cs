namespace RelayHR.Web.Entities;

public class ServiceRequest
{
    public int ServiceRequestId { get; set; }

    /// <summary>Id de la empresa (se toma del usuario autenticado). Sin FK a propósito: ver guía de integración.</summary>
    public int CompanyId { get; set; }

    public int FreelancerProfileId { get; set; }
    public FreelancerProfile? FreelancerProfile { get; set; }

    public string Description { get; set; } = string.Empty;
    public ServiceRequestStatus Status { get; set; } = ServiceRequestStatus.Sent;

    public DateTime CreatedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
}