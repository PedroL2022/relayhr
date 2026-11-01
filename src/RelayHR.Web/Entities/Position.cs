namespace RelayHR.Web.Entities;

/// <summary>
/// Puesto de trabajo (perfil del cargo). La Vacante Express copia estos datos al generarse.
/// </summary>
public class Position
{
    public int PositionId { get; set; }

    public string Title { get; set; } = string.Empty;

    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }

    // Pueden venir vacíos; en ese caso la vacante los marca como pendientes.
    public string? Description { get; set; }
    public string? Requirements { get; set; }
}
