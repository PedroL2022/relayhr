using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using RelayHR.Web.Repository;
using RelayHR.Web.Security;
using RelayHR.Web.Data.Interceptors;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RelayHR.Web.Data;
using RelayHR.Web.Entities;
using RelayHR.Web.Middleware;
using RelayHR.Web.Repositories;
using RelayHR.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Registrar el ApplicationDbContext con SQL Server LocalDB (incluido con .NET/Visual Studio)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Server=(localdb)\\mssqllocaldb;Database=RelayHRDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"));

builder.Services.AddScoped<IFreelancerRepository, FreelancerRepository>();

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddDbContext<RelayHrDbContext>((sp, options) =>
    options.UseSqlServer(sp.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection"))
        .AddInterceptors(sp.GetRequiredService<ExpressVacancyRequestInterceptor>()));
builder.Services.AddIdentity<User, IdentityRole<int>>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedAccount = false;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<RelayHrDbContext>()
    .AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});
builder.Services.AddAuthorization();
builder.Services.Configure<ChecklistOptions>(
    builder.Configuration.GetSection(ChecklistOptions.SectionName));
builder.Services.AddScoped<IEmployeeExitRepository, EmployeeExitRepository>();
builder.Services.AddScoped<IExitChecklistService, ExitChecklistService>();
builder.Services.AddScoped<IEmployeeExitService, EmployeeExitService>();

builder.Services.AddSingleton(TimeProvider.System);

// Crea la solicitud de Vacante Express cuando se guarda una salida nueva.
builder.Services.AddSingleton<ExpressVacancyRequestInterceptor>();

// Conexión a SQL Server con Entity Framework Core.
// La cadena de conexión se lee al crear el contexto, no al arrancar, para que las pruebas puedan cambiarla.
builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
{
    var connectionString = serviceProvider.GetRequiredService<IConfiguration>()
        .GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'DefaultConnection'.");

    options
        .UseSqlServer(connectionString)
        .AddInterceptors(serviceProvider.GetRequiredService<ExpressVacancyRequestInterceptor>());
});

// Generación automática de Vacantes Express.
builder.Services.Configure<ExpressVacancyOptions>(
    builder.Configuration.GetSection(ExpressVacancyOptions.SectionName));
builder.Services.AddScoped<IExpressVacancyService, ExpressVacancyService>();
builder.Services.AddSingleton<IExpressVacancyRequestProcessor, ExpressVacancyRequestProcessor>();
builder.Services.AddHostedService<ExpressVacancyWorker>();

// MVC conserva Identity.Application; JWT no cambia el esquema predeterminado.
// La configuración se resuelve al utilizar el handler, también en pruebas de integración.
builder.Services.AddAuthentication().AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IConfiguration>((options, configuration) =>
    {
        var key = configuration["Jwt:Key"];
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidIssuer = configuration["Jwt:Issuer"],
            ValidateAudience = true, ValidAudience = configuration["Jwt:Audience"],
            ValidateIssuerSigningKey = true, RequireSignedTokens = true,
            // Sin clave válida el bearer falla cerrado; las cookies siguen funcionando.
            IssuerSigningKey = !string.IsNullOrWhiteSpace(key) && key.Length >= 32
                ? new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)) : null,
            ValidateLifetime = true, ClockSkew = TimeSpan.FromMinutes(1),
            RoleClaimType = "role", NameClaimType = "name"
        };
    });
var vacancySchemes = new[] { IdentityConstants.ApplicationScheme, JwtBearerDefaults.AuthenticationScheme };
builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");
builder.Services.AddScoped<ExpressVacancyPublicationService>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.RecruiterAccess, policy => policy
        .AddAuthenticationSchemes(vacancySchemes.ToArray()).RequireAuthenticatedUser()
        .RequireRole(Roles.Recruiter, Roles.HRAdmin, IdentityRoles.HumanResources));
    options.AddPolicy(Policies.PublishVacancy, policy => policy
        .AddAuthenticationSchemes(vacancySchemes.ToArray()).RequireAuthenticatedUser()
        .RequireRole(Roles.HRAdmin, IdentityRoles.HumanResources));
});

var app = builder.Build();

// Crea la cuenta inicial solo cuando se proporcionan los tres secretos por configuración.
await IdentityBootstrapSeeder.SeedConfiguredUserAsync(app.Services, app.Configuration);

// Configure the HTTP request pipeline.
app.UseExceptionHandler();
if (!app.Environment.IsDevelopment())
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseStatusCodePages();

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();

public partial class Program { }
