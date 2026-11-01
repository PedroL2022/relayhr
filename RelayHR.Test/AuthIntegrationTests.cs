using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace RelayHR.Tests;

public class AuthTestFactory : WebApplicationFactory<Program>
{
    // Clave exclusiva para pruebas. No utiliza tu clave real.
    public const string TestKey =
        "RelayHR_TEST_ONLY_0123456789_ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijk";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting("Jwt:Key", TestKey);
        builder.UseSetting("Jwt:Issuer", "RelayHR.Tests");
        builder.UseSetting("Jwt:Audience", "RelayHR.Tests.Client");
        builder.UseSetting("Jwt:ExpirationMinutes", "60");
    }
}

public class AuthIntegrationTests : IClassFixture<AuthTestFactory>
{
    private readonly AuthTestFactory _factory;

    public AuthIntegrationTests(AuthTestFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClient()
    {
        return _factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false
            });
    }

    [Fact]
    public async Task Me_without_token_returns_401()
    {
        using var client = CreateClient();

        using var response = await client.GetAsync("/api/auth/me");

        var body = await response.Content.ReadAsStringAsync();

        Assert.True(
            response.StatusCode == HttpStatusCode.Unauthorized,
            $"Esperado: 401. Recibido: {(int)response.StatusCode}. " +
            $"Contenido: {body}");
        Assert.Contains(
            response.Headers.WwwAuthenticate,
            header => header.Scheme == "Bearer");
    }

    [Fact]
    public async Task Me_with_malformed_token_returns_401()
    {
        using var client = CreateClient();

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", "token-invalido");

        using var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("expired")]
    [InlineData("wrong-signature")]
    [InlineData("wrong-issuer")]
    [InlineData("wrong-audience")]
    public async Task Me_rejects_invalid_signed_token(string scenario)
    {
        using var client = CreateClient();

        var now = DateTime.UtcNow;

        var signingKey = scenario == "wrong-signature"
            ? new string('X', 64)
            : AuthTestFactory.TestKey;

        var token = new JwtSecurityToken(
            issuer: scenario == "wrong-issuer"
                ? "InvalidIssuer"
                : "RelayHR.Tests",
            audience: scenario == "wrong-audience"
                ? "InvalidAudience"
                : "RelayHR.Tests.Client",
            claims: new[]
            {
                new Claim("sub", Guid.NewGuid().ToString()),
                new Claim("role", "Freelancer"),
                new Claim("security_stamp", Guid.NewGuid().ToString())
            },
            notBefore: now.AddHours(-2),
            expires: scenario == "expired"
                ? now.AddMinutes(-5)
                : now.AddMinutes(30),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(signingKey)),
                SecurityAlgorithms.HmacSha256));

        var serializedToken =
            new JwtSecurityTokenHandler().WriteToken(token);

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", serializedToken);

        using var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains(
            "Debes iniciar sesión con un token válido.",
            body);
    }
}