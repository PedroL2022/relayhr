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
builder.Services.AddDbContext<RelayHrDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
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
