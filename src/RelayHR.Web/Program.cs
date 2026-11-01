using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RelayHR.Web.Data;
using RelayHR.Web.Data.Interceptors;
using RelayHR.Web.Security;
using RelayHR.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

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

// Autenticación con JWT: la API valida los tokens; quien los emite es el sistema de login.
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services
    .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IConfiguration>((options, configuration) =>
    {
        var key = configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(key) || key.Length < 32)
        {
            throw new InvalidOperationException(
                "Falta 'Jwt:Key' (mínimo 32 caracteres). En desarrollo se define en appsettings.Development.json; en producción use una variable de entorno o un gestor de secretos.");
        }

        // Los claims se leen tal como vienen en el token ("role", "name").
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = configuration["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            RoleClaimType = "role",
            NameClaimType = "name"
        };
    });

// Solo reclutadores y administradores de RRHH acceden a la bandeja.
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.RecruiterAccess, policy =>
        policy.RequireRole(Roles.Recruiter, Roles.HRAdmin));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();

// Permite que el proyecto de pruebas arranque la aplicación completa.
public partial class Program { }
