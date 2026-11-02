using System.Globalization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RelayHR.Web.Data;
using RelayHR.Web.Entities;
using RelayHR.Web.Repository;
using RelayHR.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Todos los formularios (POST) exigen el token antifalsificación; los formularios de Razor lo incluyen solos.
builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));

builder.Services.AddSingleton(TimeProvider.System);

// Base de datos: SQL Server con Entity Framework Core. La cadena de conexión está en appsettings.json.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'DefaultConnection'.")));

// Repositorios (acceso a datos) y unidad de trabajo.
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
builder.Services.AddScoped<IPositionRepository, PositionRepository>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IExitRepository, ExitRepository>();
builder.Services.AddScoped<IExitTaskRepository, ExitTaskRepository>();
builder.Services.AddScoped<IKnowledgeItemRepository, KnowledgeItemRepository>();
builder.Services.AddScoped<IVacancyRepository, VacancyRepository>();
builder.Services.AddScoped<IFreelancerRepository, FreelancerRepository>();
builder.Services.AddScoped<IServiceRequestRepository, ServiceRequestRepository>();

// Servicios con la lógica de negocio.
builder.Services.AddScoped<ExitService>();
builder.Services.AddScoped<VacancyService>();
builder.Services.AddScoped<TalentService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

// Inicio de sesión con cookie.
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Cultura fija (en-US) para que decimales y fechas de los formularios se lean igual en cualquier computador.
var culture = new CultureInfo("en-US");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(culture),
    SupportedCultures = new List<CultureInfo> { culture },
    SupportedUICultures = new List<CultureInfo> { culture }
});

// Crea o actualiza la base de datos con las migraciones y, en desarrollo, carga datos de demostración.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    if (app.Environment.IsDevelopment())
    {
        await DataSeeder.SeedAsync(
            db,
            scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>(),
            scope.ServiceProvider.GetRequiredService<TimeProvider>());
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
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
