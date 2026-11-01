using System.ComponentModel.DataAnnotations;

namespace RelayHR.Web.DTOs
{
    // Respuesta envolvente estándar
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
    }

    // Tarea 2: DTO de filtros de entrada
    public class FreelancerSearchFilterDTO
    {
        public int? CategoryId { get; set; }
        public decimal? MinRate { get; set; }
        public decimal? MaxRate { get; set; }
        public DateOnly? AvailableFrom { get; set; }
        public DateOnly? AvailableUntil { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    // Tarea 2: DTOs de salida para el listado paginado
    public class FreelancerSearchItemDTO
    {
        public int Id { get; set; }
        public string ProfessionalTitle { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal HourlyRate { get; set; }
        public string Currency { get; set; } = "DOP";
        public bool IsAvailable { get; set; }
    }

    public class FreelancerSearchResponseDataDTO
    {
        public int TotalResults { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public List<FreelancerSearchItemDTO> Freelancers { get; set; } = new();
    }

    // Tarea 3: DTOs de salida para la consulta de perfil individual
    public class FreelancerProfileResponseDTO
    {
        public int Id { get; set; }
        public string ProfessionalTitle { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Experience { get; set; } = string.Empty;
        public decimal HourlyRate { get; set; }
        public string Currency { get; set; } = "DOP";
        public List<AvailabilityItemDTO> Availability { get; set; } = new();
        public List<PortfolioItemDTO> Portfolio { get; set; } = new();
    }

    public class AvailabilityItemDTO
    {
        public DateOnly AvailableFrom { get; set; }
        public DateOnly AvailableUntil { get; set; }
    }

    public class PortfolioItemDTO
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ProjectUrl { get; set; } = string.Empty;
    }

    // HU-09: DTO para Crear Perfil (POST)
    public class CreateFreelancerProfileDTO
    {
        [Required(ErrorMessage = "El UserId es obligatorio.")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "La categoría (CategoryId) es obligatoria.")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "El título profesional es obligatorio.")]
        [MaxLength(150)]
        public string ProfessionalTitle { get; set; } = string.Empty;

        [Required(ErrorMessage = "La experiencia profesional es obligatoria.")]
        [MaxLength(2000)]
        public string Experience { get; set; } = string.Empty;

        [Required(ErrorMessage = "La tarifa por hora es obligatoria.")]
        [Range(0.01, 999999.99, ErrorMessage = "La tarifa debe ser mayor a cero.")]
        public decimal HourlyRate { get; set; }

        [Required(ErrorMessage = "La moneda es obligatoria.")]
        [MaxLength(3)]
        public string Currency { get; set; } = "DOP";

        public List<AvailabilityItemDTO> Availability { get; set; } = new();
        public List<PortfolioItemDTO> Portfolio { get; set; } = new();
    }

    // HU-09: DTO para Actualizar Perfil (PUT)
    public class UpdateFreelancerProfileDTO
    {
        [Required(ErrorMessage = "La categoría (CategoryId) es obligatoria.")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "El título profesional es obligatorio.")]
        [MaxLength(150)]
        public string ProfessionalTitle { get; set; } = string.Empty;

        [Required(ErrorMessage = "La experiencia profesional es obligatoria.")]
        [MaxLength(2000)]
        public string Experience { get; set; } = string.Empty;

        [Required(ErrorMessage = "La tarifa por hora es obligatoria.")]
        [Range(0.01, 999999.99, ErrorMessage = "La tarifa debe ser mayor a cero.")]
        public decimal HourlyRate { get; set; }

        [Required(ErrorMessage = "La moneda es obligatoria.")]
        [MaxLength(3)]
        public string Currency { get; set; } = "DOP";

        public List<AvailabilityItemDTO> Availability { get; set; } = new();
        public List<PortfolioItemDTO> Portfolio { get; set; } = new();
    }
}