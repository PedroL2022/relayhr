using System.ComponentModel.DataAnnotations;

namespace RelayHR.Web.Models.DTOs.ServiceRequests;

public class CreateServiceRequestDTO
{
    [Range(1, int.MaxValue, ErrorMessage = "El profesional es obligatorio.")]
    public int FreelancerProfileId { get; set; }

    [Required(ErrorMessage = "La descripción del servicio es obligatoria.")]
    [StringLength(1000, ErrorMessage = "La descripción no puede superar 1000 caracteres.")]
    public string Description { get; set; } = string.Empty;
}

public class RespondServiceRequestDTO
{
    [Required(ErrorMessage = "La respuesta es obligatoria.")]
    public string Response { get; set; } = string.Empty;
}

public class ServiceRequestCreatedDTO
{
    public int ServiceRequestId { get; set; }
    public int CompanyId { get; set; }
    public int FreelancerProfileId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class ServiceRequestRespondedDTO
{
    public int ServiceRequestId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime RespondedAt { get; set; }
}

public class ServiceRequestConfirmedDTO
{
    public int ServiceRequestId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ConfirmedAt { get; set; }
}

public class ServiceRequestResponseDTO
{
    public int ServiceRequestId { get; set; }
    public int CompanyId { get; set; }
    public int FreelancerProfileId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
}