using Microsoft.EntityFrameworkCore;
using RelayHR.Web.Data.ServiceRequests;

namespace RelayHR.Web.Services.ServiceRequests;

public static class ServiceRequestModuleExtensions
{
    public const string DefaultConnection =
        "Server=(localdb)\\MSSQLLocalDB;Database=RelayHR;Trusted_Connection=True;TrustServerCertificate=True";

    public static IServiceCollection AddServiceRequestModule(
        this IServiceCollection services, IConfiguration configuration)
    {
        // >>> Estas 2 líneas son TEMPORALES: al unir ramas se reemplazan por el DbContext principal.
        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? DefaultConnection;
        services.AddDbContext<ServiceRequestDbContext>(o => o.UseSqlServer(connectionString));
        // <<<

        services.AddScoped<IServiceRequestDbContext>(sp => sp.GetRequiredService<ServiceRequestDbContext>());
        services.AddScoped<IServiceRequestService, ServiceRequestService>();
        return services;
    }
}