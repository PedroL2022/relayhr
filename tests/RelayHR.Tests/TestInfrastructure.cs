using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RelayHR.Web.Data.ServiceRequests;

namespace RelayHR.Tests;

/// <summary>Autenticación falsa: X-Test-UserId + X-Test-Role. Sin headers => 401.</summary>
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public new const string Scheme = "Test";

    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder) : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-UserId", out var userId) ||
            !Request.Headers.TryGetValue("X-Test-Role", out var role))
            return Task.FromResult(AuthenticateResult.NoResult());

        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role.ToString())
        }, Scheme);

        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

public class AuthenticationStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) =>
        app => { app.UseAuthentication(); next(app); };
}

public class RelayHrFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public RelayHrFactory() => _connection.Open();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Quita TODO lo registrado para el DbContext (incluye IDbContextOptionsConfiguration<T> de EF 9).
            var toRemove = services.Where(d =>
                d.ServiceType == typeof(ServiceRequestDbContext) ||
                d.ServiceType == typeof(IServiceRequestDbContext) ||
                (d.ServiceType.IsGenericType &&
                 d.ServiceType.GenericTypeArguments.Contains(typeof(ServiceRequestDbContext)))).ToList();
            foreach (var d in toRemove) services.Remove(d);

            services.AddDbContext<ServiceRequestDbContext>(o => o.UseSqlite(_connection));
            services.AddScoped<IServiceRequestDbContext>(sp => sp.GetRequiredService<ServiceRequestDbContext>());

            services.AddAuthentication(TestAuthHandler.Scheme)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.Scheme, _ => { });
            services.AddTransient<IStartupFilter, AuthenticationStartupFilter>();
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<ServiceRequestDbContext>().Database.EnsureCreated();
        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection.Dispose();
    }
}