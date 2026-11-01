using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace RelayHR.Web.Data;
// Generación de migraciones sin iniciar el worker ni acceder a la base de datos.
public sealed class RelayHrDbContextFactory : IDesignTimeDbContextFactory<RelayHrDbContext>
{
    public RelayHrDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true).AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables().Build();
        var connection = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=RelayHR;Trusted_Connection=True;TrustServerCertificate=True";
        return new(new DbContextOptionsBuilder<RelayHrDbContext>().UseSqlServer(connection).Options);
    }
}
