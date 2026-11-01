using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using RelayHR.Web.Entities.KnowledgeTransfer;
using RelayHR.Web.Entities.Stubs;

namespace RelayHR.Web.Data.KnowledgeTransfer;

// TEMPORAL: DbContext provisional del módulo. Al unir con el proyecto, el DbContext principal
// debe implementar IKnowledgeTransferDbContext y este archivo se borra.
public class KnowledgeTransferDbContext : DbContext, IKnowledgeTransferDbContext
{
    public KnowledgeTransferDbContext(DbContextOptions<KnowledgeTransferDbContext> options) : base(options) { }

    public DbSet<EmployeeExit> EmployeeExits => Set<EmployeeExit>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<KnowledgeTransferProject> KnowledgeTransferProjects => Set<KnowledgeTransferProject>();
    public DbSet<KnowledgeTransferTask> KnowledgeTransferTasks => Set<KnowledgeTransferTask>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new EmployeeExitStubConfiguration());
        modelBuilder.ApplyConfiguration(new AppUserStubConfiguration());
        modelBuilder.ApplyConfiguration(new KnowledgeTransferProjectConfiguration());
        modelBuilder.ApplyConfiguration(new KnowledgeTransferTaskConfiguration());
    }
}

// Permite ejecutar "dotnet ef" leyendo la misma cadena de conexión que usa la aplicación.
public class KnowledgeTransferDbContextFactory : IDesignTimeDbContextFactory<KnowledgeTransferDbContext>
{
    public KnowledgeTransferDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? "Server=localhost;Database=RelayHR;Trusted_Connection=True;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<KnowledgeTransferDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new KnowledgeTransferDbContext(options);
    }
}
