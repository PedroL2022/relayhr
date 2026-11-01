using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RelayHR.Web.Data;
using RelayHR.Web.DTOs.Auth;
using RelayHR.Web.Entities;

namespace RelayHR.Web.Services;

public class AuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;

    private readonly JwtTokenService _jwtTokenService;


    public AuthService(
    ApplicationDbContext context,
    IPasswordHasher<User> passwordHasher,
    JwtTokenService jwtTokenService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<(bool Success, string Message)> RegisterAsync(
        RegisterUserDTO dto,
        CancellationToken cancellationToken = default)
    {
        // También se valida aquí para impedir asignaciones privilegiadas.
        if (dto.Role != "Company" && dto.Role != "Freelancer")
        {
            return (false,
                "El registro público solo permite Company o Freelancer.");
        }

        var email = dto.Email.Trim();

        if (email.Length > 254 ||
            !new System.ComponentModel.DataAnnotations.EmailAddressAttribute()
                .IsValid(email))
        {
            return (false, "El correo no tiene un formato válido.");
        }

        if (string.IsNullOrWhiteSpace(dto.Password) ||
            dto.Password.Length < 12 ||
            dto.Password.Length > 128)
        {
            return (false,
                "La contraseña debe tener entre 12 y 128 caracteres.");
        }

        var normalizedEmail = email.ToUpperInvariant();

        var emailExists = await _context.Users.AnyAsync(
            user => user.NormalizedEmail == normalizedEmail,
            cancellationToken);

        if (emailExists)
        {
            return (false, "El correo ya está registrado.");
        }

        var role = await _context.Roles.SingleOrDefaultAsync(
            role => role.Name == dto.Role,
            cancellationToken);

        if (role is null)
        {
            throw new InvalidOperationException(
                "No se encontró el rol configurado para el registro.");
        }

        var user = new User
        {
            Email = email,
            NormalizedEmail = normalizedEmail
        };

        user.PasswordHash = _passwordHasher.HashPassword(
            user, dto.Password);

        user.UserRoles.Add(new UserRole
        {
            UserId = user.Id,
            User = user,
            RoleId = role.Id,
            Role = role
        });

        _context.Users.Add(user);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is SqlException
            {
                Number: 2601 or 2627
            })
        {
            // El índice único también protege registros simultáneos.
            _context.Entry(user).State = EntityState.Detached;

            return (false, "No se pudo registrar: el correo ya está registrado.");
        }

        return (true, "Usuario registrado correctamente.");
    }

    public async Task<AuthResponseDTO?> LoginAsync(
    LoginDTO dto,
    CancellationToken cancellationToken = default)
    {
        var normalizedEmail = dto.Email.Trim().ToUpperInvariant();

        // La transacción serializable protege el contador de intentos
        // frente a solicitudes simultáneas sobre la misma cuenta.
        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable,
                cancellationToken);

        var user = await _context.Users
            .Include(user => user.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .SingleOrDefaultAsync(
                user => user.NormalizedEmail == normalizedEmail,
                cancellationToken);

        if (user is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return null;
        }

        var now = DateTimeOffset.UtcNow;

        if (!user.IsActive || user.LockoutEnd > now)
        {
            await transaction.CommitAsync(cancellationToken);
            return null;
        }

        // Al terminar el bloqueo comienza un nuevo ciclo de intentos.
        if (user.LockoutEnd.HasValue)
        {
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
        }

        var verification = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            dto.Password);

        if (verification == PasswordVerificationResult.Failed)
        {
            user.FailedLoginAttempts++;

            if (user.FailedLoginAttempts >= 5)
            {
                user.LockoutEnd = now.AddMinutes(15);
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return null;
        }

        if (user.UserRoles.Count == 0)
        {
            await transaction.CommitAsync(cancellationToken);
            return null;
        }

        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(
                user, dto.Password);
        }

        var response = _jwtTokenService.GenerateToken(user);

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return response;
    }

}
