using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RelayHR.Web.DTOs.Auth;
using RelayHR.Web.Services;
using Microsoft.AspNetCore.RateLimiting;

namespace RelayHR.Web.Controllers;

[ApiController]
[Route("api/auth")]
[Authorize]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [EnableRateLimiting("AuthRequests")]
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterUserDTO dto,
        CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterAsync(
            dto, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new
            {
                success = false,
                message = result.Message,
                data = (object?)null
            });
        }

        return StatusCode(StatusCodes.Status201Created, new
        {
            success = true,
            message = result.Message,
            data = (object?)null
        });
    }

    [AllowAnonymous]
    [EnableRateLimiting("AuthRequests")]
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginDTO dto,
        CancellationToken cancellationToken)
    {
        var response = await _authService.LoginAsync(
            dto, cancellationToken);

        if (response is null)
        {
            return Unauthorized(new
            {
                success = false,
                message = "Correo o contraseña incorrectos.",
                data = (object?)null
            });
        }

        return Ok(new
        {
            success = true,
            message = "Inicio de sesión exitoso.",
            data = response
        });
    }

    [HttpGet("me")]
    public IActionResult Me()
    {
        var roles = User.FindAll("role")
            .Select(claim => claim.Value)
            .Distinct()
            .OrderBy(role => role)
            .ToArray();

        return Ok(new
        {
            success = true,
            message = "Sesión autenticada.",
            data = new
            {
                userId = User.FindFirst("sub")?.Value,
                roles
            }
        });
    }
}