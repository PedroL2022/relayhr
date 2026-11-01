using System.ComponentModel.DataAnnotations;

namespace RelayHR.Web.DTOs.Auth;

public class LoginDTO
{
    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    [MaxLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [MaxLength(128)]
    public string Password { get; set; } = string.Empty;
}