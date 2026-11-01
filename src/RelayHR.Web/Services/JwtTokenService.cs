using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using RelayHR.Web.DTOs.Auth;
using RelayHR.Web.Entities;

namespace RelayHR.Web.Services;

public class JwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public AuthResponseDTO GenerateToken(User user)
    {
        var key = GetRequiredSetting("Jwt:Key");
        var issuer = GetRequiredSetting("Jwt:Issuer");
        var audience = GetRequiredSetting("Jwt:Audience");

        if (Encoding.UTF8.GetByteCount(key) < 64)
        {
            throw new InvalidOperationException(
                "La clave JWT debe tener al menos 64 bytes.");
        }

        if (!int.TryParse(
                _configuration["Jwt:ExpirationMinutes"],
                out var minutes)
            || minutes < 1
            || minutes > 1440)
        {
            throw new InvalidOperationException(
                "Jwt:ExpirationMinutes debe estar entre 1 y 1440.");
        }

        var roles = user.UserRoles
            .Select(userRole => userRole.Role.Name)
            .Distinct()
            .OrderBy(role => role)
            .ToArray();

        if (roles.Length == 0)
        {
            throw new InvalidOperationException(
                "El usuario no tiene roles asignados.");
        }

        var now = DateTime.UtcNow;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("security_stamp", user.SecurityStamp.ToString())
        };

        claims.AddRange(
            roles.Select(role => new Claim("role", role)));

        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(key));

        var credentials = new SigningCredentials(
            signingKey,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(minutes),
            signingCredentials: credentials);

        return new AuthResponseDTO
        {
            AccessToken = new JwtSecurityTokenHandler()
                .WriteToken(token),
            TokenType = "Bearer",
            ExpiresIn = minutes * 60,
            Role = roles[0],
            Roles = roles
        };
    }

    private string GetRequiredSetting(string name)
    {
        var value = _configuration[name];

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Falta configurar {name}.");
        }

        return value;
    }
}