using System.ComponentModel.DataAnnotations;

namespace RelayHR.Web.DTOs.Auth;

public class RegisterUserDTO
{
    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    [MaxLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(128, MinimumLength = 12,
        ErrorMessage = "La contraseña debe tener entre 12 y 128 caracteres.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "El rol es obligatorio.")]
    [RegularExpression("^(Company|Freelancer)$",
        ErrorMessage = "El registro público solo permite Company o Freelancer.")]
    public string Role { get; set; } = string.Empty;
}