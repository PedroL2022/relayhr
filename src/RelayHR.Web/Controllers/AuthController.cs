using User = RelayHR.Web.Entities.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RelayHR.Web.Entities;
using RelayHR.Web.Models;

namespace RelayHR.Web.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    UserManager<User> userManager,
    SignInManager<User> signInManager) : ControllerBase
{
    /// <summary>Inicia sesión y emite una cookie de autenticación segura.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType<ApiResponse<string>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<string>>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Credenciales inválidas",
                Detail = "El correo o la contraseña no son válidos."
            });
        }

        var result = await signInManager.PasswordSignInAsync(
            user,
            request.Password,
            isPersistent: false,
            lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Credenciales inválidas",
                Detail = "El correo o la contraseña no son válidos."
            });
        }

        cancellationToken.ThrowIfCancellationRequested();
        return Ok(new ApiResponse<string>(
            true,
            "Sesión iniciada correctamente.",
            "La cookie de autenticación se envió en la respuesta."));
    }

    /// <summary>Cierra la sesión actual y elimina la cookie de autenticación.</summary>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType<ApiResponse<string>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<string>>> Logout()
    {
        await signInManager.SignOutAsync();
        return Ok(new ApiResponse<string>(
            true,
            "Sesión cerrada correctamente.",
            "La cookie de autenticación se eliminó."));
    }
}
