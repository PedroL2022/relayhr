using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
namespace RelayHR.Web.Dtos;

// PUT parcial por compatibilidad: omitido conserva; null explícito limpia el campo.
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class UpdateExpressVacancyDTO
{
    private string? jobTitle, jobDescription, requirements;
    private int? departmentId;
    [MaxLength(150, ErrorMessage = "El cargo no puede superar 150 caracteres.")]
    public string? JobTitle { get => jobTitle; set { jobTitle = value; HasJobTitle = true; } }
    public int? DepartmentId { get => departmentId; set { departmentId = value; HasDepartmentId = true; } }
    [MaxLength(4000, ErrorMessage = "La descripción y los requisitos no pueden superar 4000 caracteres.")]
    public string? JobDescription { get => jobDescription; set { jobDescription = value; HasJobDescription = true; } }
    [MaxLength(4000, ErrorMessage = "La descripción y los requisitos no pueden superar 4000 caracteres.")]
    public string? Requirements { get => requirements; set { requirements = value; HasRequirements = true; } }
    [JsonIgnore] public bool HasJobTitle { get; private set; }
    [JsonIgnore] public bool HasDepartmentId { get; private set; }
    [JsonIgnore] public bool HasJobDescription { get; private set; }
    [JsonIgnore] public bool HasRequirements { get; private set; }
}
