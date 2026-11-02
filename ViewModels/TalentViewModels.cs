using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using RelayHR.Web.Entities;

namespace RelayHR.Web.ViewModels;

public class VacancyEditViewModel
{
    public int ExpressVacancyId { get; set; }

    [StringLength(150, ErrorMessage = "El título no puede pasar de 150 caracteres.")]
    [Display(Name = "Título del puesto")]
    public string? JobTitle { get; set; }

    [Display(Name = "Departamento")]
    public int? DepartmentId { get; set; }

    [StringLength(4000, ErrorMessage = "La descripción no puede pasar de 4000 caracteres.")]
    [Display(Name = "Descripción")]
    public string? JobDescription { get; set; }

    [StringLength(4000, ErrorMessage = "Los requisitos no pueden pasar de 4000 caracteres.")]
    [Display(Name = "Requisitos")]
    public string? Requirements { get; set; }

    // Solo lectura: se vuelve a cargar de la base de datos en cada visita.
    public ExpressVacancy? Vacancy { get; set; }
    public List<SelectListItem> Departments { get; set; } = new();
}

public class DirectoryViewModel
{
    [Display(Name = "Categoría")]
    public string? Category { get; set; }

    [Range(0, 1000000, ErrorMessage = "La tarifa debe ser un número positivo.")]
    [Display(Name = "Tarifa máxima por hora (RD$)")]
    public decimal? MaxHourlyRate { get; set; }

    [Display(Name = "Solo disponibles")]
    public bool OnlyAvailable { get; set; }

    public List<FreelancerProfile> Results { get; set; } = new();
}

public class FreelancerProfileViewModel
{
    [Required(ErrorMessage = "Elige tu categoría profesional.")]
    [Display(Name = "Categoría profesional")]
    public string Category { get; set; } = string.Empty;

    [Range(0, 60, ErrorMessage = "Los años de experiencia deben estar entre 0 y 60.")]
    [Display(Name = "Años de experiencia")]
    public int ExperienceYears { get; set; }

    [Required(ErrorMessage = "Cuéntanos tu experiencia.")]
    [StringLength(2000, ErrorMessage = "El resumen no puede pasar de 2000 caracteres.")]
    [Display(Name = "Resumen de experiencia")]
    public string ExperienceSummary { get; set; } = string.Empty;

    [Url(ErrorMessage = "El enlace del portafolio no es válido.")]
    [StringLength(300, ErrorMessage = "El enlace no puede pasar de 300 caracteres.")]
    [Display(Name = "Enlace a tu portafolio")]
    public string? PortfolioUrl { get; set; }

    [Range(1, 1000000, ErrorMessage = "La tarifa debe ser mayor que cero.")]
    [Display(Name = "Tarifa por hora (RD$)")]
    public decimal HourlyRate { get; set; }

    [Display(Name = "Estoy disponible para nuevos trabajos")]
    public bool IsAvailable { get; set; } = true;

    [StringLength(200, ErrorMessage = "La nota no puede pasar de 200 caracteres.")]
    [Display(Name = "Nota de disponibilidad (opcional)")]
    public string? AvailabilityNote { get; set; }
}

/// <summary>Resumen que se muestra en la página de inicio, según el rol.</summary>
public class DashboardViewModel
{
    public int ExitsInProgress { get; set; }
    public int VacanciesToPublish { get; set; }
    public int UnassignedItems { get; set; }
    public int ActiveRequests { get; set; }
    public bool FreelancerHasProfile { get; set; }
    public int IncomingRequests { get; set; }
}
