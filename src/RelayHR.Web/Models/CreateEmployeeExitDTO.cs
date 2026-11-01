using System.ComponentModel.DataAnnotations;

namespace RelayHR.Web.Models;

/// <summary>Datos necesarios para registrar la salida de un empleado.</summary>
public sealed record CreateEmployeeExitDTO
{
    [Range(1, int.MaxValue)]
    public required int EmployeeId { get; init; }

    [Required]
    public required DateOnly EffectiveDate { get; init; }
}
