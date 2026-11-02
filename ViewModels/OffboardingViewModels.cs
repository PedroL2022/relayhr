using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using RelayHR.Web.Entities;

namespace RelayHR.Web.ViewModels;

public class EmployeeFormViewModel
{
    [Required(ErrorMessage = "Escribe el nombre del empleado.")]
    [StringLength(150, ErrorMessage = "El nombre no puede pasar de 150 caracteres.")]
    [Display(Name = "Nombre completo")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Puesto")]
    public int? PositionId { get; set; }

    [Display(Name = "Departamento")]
    public int? DepartmentId { get; set; }

    public List<SelectListItem> Positions { get; set; } = new();
    public List<SelectListItem> Departments { get; set; } = new();
}

public class PositionFormViewModel
{
    [Required(ErrorMessage = "Escribe el título del puesto.")]
    [StringLength(150, ErrorMessage = "El título no puede pasar de 150 caracteres.")]
    [Display(Name = "Título del puesto")]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Departamento")]
    public int? DepartmentId { get; set; }

    [StringLength(4000, ErrorMessage = "La descripción no puede pasar de 4000 caracteres.")]
    [Display(Name = "Descripción")]
    public string? Description { get; set; }

    [StringLength(4000, ErrorMessage = "Los requisitos no pueden pasar de 4000 caracteres.")]
    [Display(Name = "Requisitos")]
    public string? Requirements { get; set; }

    public List<SelectListItem> Departments { get; set; } = new();
}

public class ExitFormViewModel
{
    [Required(ErrorMessage = "Selecciona el empleado.")]
    [Display(Name = "Empleado")]
    public int? EmployeeId { get; set; }

    [Required(ErrorMessage = "Indica la fecha efectiva de salida.")]
    [Display(Name = "Fecha efectiva de salida")]
    public DateOnly? ExitDate { get; set; }

    [StringLength(500, ErrorMessage = "El motivo no puede pasar de 500 caracteres.")]
    [Display(Name = "Motivo (opcional)")]
    public string? Reason { get; set; }

    public List<SelectListItem> Employees { get; set; } = new();
}

public class KnowledgeItemFormViewModel
{
    public int ExitId { get; set; }

    [Required(ErrorMessage = "Escribe el nombre del proyecto o tarea.")]
    [StringLength(200, ErrorMessage = "El nombre no puede pasar de 200 caracteres.")]
    [Display(Name = "Proyecto o tarea")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Describe en qué consiste.")]
    [StringLength(2000, ErrorMessage = "La descripción no puede pasar de 2000 caracteres.")]
    [Display(Name = "Descripción")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Indica el estado actual.")]
    [StringLength(200, ErrorMessage = "El estado no puede pasar de 200 caracteres.")]
    [Display(Name = "Estado actual")]
    public string CurrentStatus { get; set; } = string.Empty;

    [Display(Name = "Responsable temporal")]
    public int? AssignedToEmployeeId { get; set; }
}

/// <summary>Datos de la pantalla de traspaso de conocimiento de una salida.</summary>
public class HandoverViewModel
{
    public EmployeeExit Exit { get; set; } = null!;
    public List<KnowledgeItem> Items { get; set; } = new();
    public List<SelectListItem> Employees { get; set; } = new();
    public KnowledgeItemFormViewModel Form { get; set; } = new();
}

public class AssigneeViewModel
{
    public Employee Employee { get; set; } = null!;
    public List<KnowledgeItem> Items { get; set; } = new();
}
