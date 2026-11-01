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
}