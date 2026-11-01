namespace RelayHR.Web.DTOs.Auth;

public class AuthResponseDTO
{
    public string AccessToken { get; set; } = string.Empty;

    public string TokenType { get; set; } = "Bearer";

    public int ExpiresIn { get; set; }

    public string Role { get; set; } = string.Empty;

    public string[] Roles { get; set; } = Array.Empty<string>();
}