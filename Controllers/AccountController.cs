using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RelayHR.Web.Entities;
using RelayHR.Web.Repository;
using RelayHR.Web.Security;
using RelayHR.Web.ViewModels;

namespace RelayHR.Web.Controllers;

/// <summary>
/// Registro, inicio y cierre de sesión. La sesión se guarda en una cookie.
/// </summary>
[AllowAnonymous]
public class AccountController : Controller
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher<User> _hasher;
    private readonly TimeProvider _time;

    public AccountController(IUserRepository users, IUnitOfWork unitOfWork, IPasswordHasher<User> hasher, TimeProvider time)
    {
        _users = users;
        _unitOfWork = unitOfWork;
        _hasher = hasher;
        _time = time;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null) =>
        View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var email = model.Email.Trim().ToLowerInvariant();
        var user = await _users.GetByEmailAsync(email);

        var valid = user is not null &&
            _hasher.VerifyHashedPassword(user, user.PasswordHash, model.Password) != PasswordVerificationResult.Failed;

        if (!valid)
        {
            // El mismo mensaje para correo y contraseña: no revela cuál de los dos falló.
            ModelState.AddModelError(string.Empty, "Correo o contraseña incorrectos.");
            return View(model);
        }

        await SignInAsync(user!);

        return !string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl)
            ? LocalRedirect(model.ReturnUrl)
            : RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!Roles.All.Contains(model.Role))
        {
            ModelState.AddModelError(nameof(model.Role), "El tipo de cuenta no es válido.");
        }

        if (Roles.IsCompanyRole(model.Role) && string.IsNullOrWhiteSpace(model.CompanyName))
        {
            ModelState.AddModelError(nameof(model.CompanyName), "Escribe el nombre de la empresa.");
        }

        var email = model.Email.Trim().ToLowerInvariant();

        if (await _users.EmailExistsAsync(email))
        {
            ModelState.AddModelError(nameof(model.Email), "Ya existe una cuenta con este correo.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new User
        {
            FullName = model.FullName.Trim(),
            Email = email,
            Role = model.Role,
            CompanyName = Roles.IsCompanyRole(model.Role) ? model.CompanyName!.Trim() : null,
            CreatedAt = _time.GetUtcNow().UtcDateTime
        };
        user.PasswordHash = _hasher.HashPassword(user, model.Password);

        _users.Add(user);
        await _unitOfWork.SaveChangesAsync();

        await SignInAsync(user);
        TempData["Success"] = "Tu cuenta fue creada. ¡Bienvenido a RelayHR!";
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();

    private Task SignInAsync(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        return HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));
    }
}
