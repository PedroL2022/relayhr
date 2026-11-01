using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RelayHR.Web.Data.Interceptors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RelayHR.Web.Data;
using RelayHR.Web.Entities;
using RelayHR.Web.Services;
using System.Net.Http.Json;

namespace RelayHR.Web.Tests.Integration;

internal sealed class TestApplication : IAsyncDisposable
{
    private const string TestPassword = "RelayHr-Test-123!";

    private readonly SqliteConnection _connection;
    private readonly TestWebApplicationFactory _factory;

    private TestApplication(SqliteConnection connection, TestWebApplicationFactory factory)
    {
        _connection = connection;
        _factory = factory;
        Client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    public HttpClient Client { get; }

    public WebApplicationFactory<Program> Factory => _factory;

    public static async Task<TestApplication> StartAsync(bool failChecklistGeneration = false, bool enableJwt = false)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var factory = new TestWebApplicationFactory(connection, failChecklistGeneration, enableJwt);
        var application = new TestApplication(connection, factory);

        try
        {
            await application.InitializeDatabaseAsync();
            await application.LoginAsync("hr@example.test");
            return application;
        }
        catch
        {
            await application.DisposeAsync();
            throw;
        }
    }

    public HttpClient CreateUnauthenticatedClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

    public async Task LoginAsync(string email)
    {
        using var response = await Client.PostAsJsonAsync(
            "/api/auth/login",
            new { email, password = TestPassword });
        response.EnsureSuccessStatusCode();
    }

    public async Task<T> InScopeAsync<T>(Func<RelayHrDbContext, Task<T>> action)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RelayHrDbContext>();
        return await action(dbContext);
    }

    private async Task InitializeDatabaseAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        var dbContext = services.GetRequiredService<RelayHrDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        dbContext.Employees.Add(new Employee { Id = 15, Name = "Ana Ejemplo" });
        await dbContext.SaveChangesAsync();

        var userManager = services.GetRequiredService<UserManager<User>>();
        await CreateUserAsync(userManager, 101, "tech@example.test", "Tecnología");
        await CreateUserAsync(userManager, 102, "people@example.test", "Recursos Humanos");
        await CreateUserAsync(userManager, 103, "finance@example.test", "Finanzas");
        var hrUser = await CreateUserAsync(userManager, 104, "hr@example.test", "Administradora RR. HH.");
        var roleResult = await userManager.AddToRoleAsync(hrUser, IdentityRoles.HumanResources);
        EnsureSucceeded(roleResult);
        await CreateUserAsync(userManager, 105, "employee@example.test", "Usuario sin permisos");
    }

    private static async Task<User> CreateUserAsync(
        UserManager<User> userManager,
        int id,
        string email,
        string name)
    {
        var user = new User
        {
            Id = id,
            Name = name,
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(user, TestPassword);
        EnsureSucceeded(result);
        return user;
    }

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(
                "; ",
                result.Errors.Select(error => error.Description)));
        }
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        _factory.Dispose();
        await _connection.DisposeAsync();
    }

    private sealed class TestWebApplicationFactory(
        SqliteConnection connection,
        bool failChecklistGeneration, bool enableJwt) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureLogging(logging => logging.ClearProviders());
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = enableJwt ? "HU05-test-only-signing-key-at-least-32-characters" : null,
                    ["Jwt:Issuer"] = "relayhr-tests", ["Jwt:Audience"] = "relayhr-tests",
                    ["Checklist:ResponsibleUserIds:Technology"] = "101",
                    ["Checklist:ResponsibleUserIds:HumanResources"] = "102",
                    ["Checklist:ResponsibleUserIds:Finance"] = "103"
                }));

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IDbContextOptionsConfiguration<RelayHrDbContext>>();
                services.RemoveAll<DbContextOptions<RelayHrDbContext>>();
                services.RemoveAll<RelayHrDbContext>();
                services.AddDataProtection().UseEphemeralDataProtectionProvider();
                services.AddDbContext<RelayHrDbContext>((sp, options) => options.UseSqlite(connection)
                    .AddInterceptors(sp.GetRequiredService<ExpressVacancyRequestInterceptor>()));
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<AppDbContext>();
                services.AddDbContext<AppDbContext>(options => options.UseSqlite(connection));
                foreach (var descriptor in services.Where(d => d.ServiceType == typeof(IHostedService)
                    && d.ImplementationType == typeof(ExpressVacancyWorker)).ToArray()) services.Remove(descriptor);

                if (failChecklistGeneration)
                {
                    services.RemoveAll<IExitChecklistService>();
                    services.AddSingleton<IExitChecklistService, FailingChecklistService>();
                }
            });
        }
    }

    private sealed class FailingChecklistService : IExitChecklistService
    {
        public Task<ExitChecklist> GenerateAsync(
            EmployeeExit employeeExit,
            CancellationToken cancellationToken) =>
            throw new ChecklistConfigurationException("Falla de generación simulada.");
    }
}
