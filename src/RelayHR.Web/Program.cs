using Microsoft.EntityFrameworkCore;
using RelayHR.Web.Data;
using RelayHR.Web.Repository;
var builder = WebApplication.CreateBuilder(args);

// Registrar el ApplicationDbContext con SQL Server LocalDB (incluido con .NET/Visual Studio)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Server=(localdb)\\mssqllocaldb;Database=RelayHRDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"));

builder.Services.AddScoped<IFreelancerRepository, FreelancerRepository>();

// Add services to the container.
builder.Services.AddControllersWithViews();

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


// Registrar el Patrón Repositorio para que los controladores puedan usarlo
builder.Services.AddScoped<IFreelancerRepository, FreelancerRepository>();
