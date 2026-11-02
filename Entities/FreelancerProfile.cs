namespace RelayHR.Web.Entities;

/// <summary>Categorías profesionales disponibles para los perfiles y los filtros.</summary>
public static class FreelancerCategories
{
    public static readonly IReadOnlyList<string> All = new List<string>
    {
        "Desarrollo de software",
        "Diseño gráfico y UX",
        "Soporte técnico TI",
        "Contabilidad y finanzas",
        "Recursos humanos",
        "Marketing y ventas",
        "Administración",
        "Otro"
    };
}

/// <summary>
/// Perfil público de un trabajador independiente para el directorio de talento interino.
/// </summary>
public class FreelancerProfile
{
    public int FreelancerProfileId { get; set; }

    // Cada usuario freelancer tiene como máximo un perfil.
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string Category { get; set; } = string.Empty;

    public int ExperienceYears { get; set; }

    public string ExperienceSummary { get; set; } = string.Empty;

    public string? PortfolioUrl { get; set; }

    // Tarifa por hora en pesos dominicanos.
    public decimal HourlyRate { get; set; }

    public bool IsAvailable { get; set; }

    public string? AvailabilityNote { get; set; }

    public DateTime UpdatedAt { get; set; }

    public void Apply(
        string category,
        int experienceYears,
        string experienceSummary,
        string? portfolioUrl,
        decimal hourlyRate,
        bool isAvailable,
        string? availabilityNote,
        DateTime now)
    {
        Category = category;
        ExperienceYears = experienceYears;
        ExperienceSummary = experienceSummary.Trim();
        PortfolioUrl = string.IsNullOrWhiteSpace(portfolioUrl) ? null : portfolioUrl.Trim();
        HourlyRate = hourlyRate;
        IsAvailable = isAvailable;
        AvailabilityNote = string.IsNullOrWhiteSpace(availabilityNote) ? null : availabilityNote.Trim();
        UpdatedAt = now;
    }
}
