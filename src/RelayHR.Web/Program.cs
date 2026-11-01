using Microsoft.EntityFrameworkCore;
using RelayHR.Web.Data;
using RelayHR.Web.Data.Interceptors;
using RelayHR.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Conexión a SQL Server con Entity Framework Core.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'DefaultConnection'.");

builder.Services.AddSingleton(TimeProvider.System);

// Crea la solicitud de Vacante Express cuando se guarda una salida nueva.
builder.Services.AddSingleton<ExpressVacancyRequestInterceptor>();

builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
    options
        .UseSqlServer(connectionString)
        .AddInterceptors(serviceProvider.GetRequiredService<ExpressVacancyRequestInterceptor>()));

// Generación automática de Vacantes Express (HU-03).
builder.Services.Configure<ExpressVacancyOptions>(
builder.Configuration.GetSection(ExpressVacancyOptions.SectionName));
builder.Services.AddScoped<IExpressVacancyService, ExpressVacancyService>();
builder.Services.AddSingleton<IExpressVacancyRequestProcessor, ExpressVacancyRequestProcessor>();
builder.Services.AddHostedService<ExpressVacancyWorker>();

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

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
