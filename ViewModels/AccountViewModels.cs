using System.ComponentModel.DataAnnotations;

namespace RelayHR.Web.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Escribe tu correo.")]
    [EmailAddress(ErrorMessage = "El correo no es válido.")]
    [Display(Name = "Correo electrónico")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escribe tu contraseña.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Escribe tu nombre completo.")]
    [StringLength(150, ErrorMessage = "El nombre no puede pasar de 150 caracteres.")]
    [Display(Name = "Nombre completo")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escribe tu correo.")]
    [EmailAddress(ErrorMessage = "El correo no es válido.")]
    [StringLength(256, ErrorMessage = "El correo no puede pasar de 256 caracteres.")]
    [Display(Name = "Correo electrónico")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Elige un tipo de cuenta.")]
    [Display(Name = "Tipo de cuenta")]
    public string Role { get; set; } = string.Empty;

    [StringLength(150, ErrorMessage = "El nombre de la empresa no puede pasar de 150 caracteres.")]
    [Display(Name = "Nombre de la empresa")]
    public string? CompanyName { get; set; }

    [Required(ErrorMessage = "Escribe una contraseña.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirma tu contraseña.")]
    [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar contraseña")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
