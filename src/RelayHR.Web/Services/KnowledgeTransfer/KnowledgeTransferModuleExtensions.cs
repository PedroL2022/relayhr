using Microsoft.EntityFrameworkCore;
using RelayHR.Web.Data.KnowledgeTransfer;

namespace RelayHR.Web.Services.KnowledgeTransfer;

public static class KnowledgeTransferModuleExtensions
{
    public static IServiceCollection AddKnowledgeTransferModule(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Falta la cadena de conexión 'DefaultConnection'.");

        // TEMPORAL (inicio): al unir con el DbContext principal, borrar estas 2 líneas y registrar
        // IKnowledgeTransferDbContext apuntando al DbContext principal.
        services.AddDbContext<KnowledgeTransferDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IKnowledgeTransferDbContext>(sp => sp.GetRequiredService<KnowledgeTransferDbContext>());
        // TEMPORAL (fin)

        services.AddScoped<IKnowledgeTransferService, KnowledgeTransferService>();
        return services;
    }
}
