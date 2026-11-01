using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using RelayHR.Web.Entities;
using RelayHR.Web.Services.ServiceRequests;

namespace RelayHR.Web.Data.ServiceRequests;

// DbContext provisional del módulo. Al unir ramas se elimina y se usa el DbContext principal.
public class ServiceRequestDbContext : DbContext, IServiceRequestDbContext
{
    public ServiceRequestDbContext(DbContextOptions<ServiceRequestDbContext> options) : base(options) { }

    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();
    public DbSet<FreelancerProfile> FreelancerProfiles => Set<FreelancerProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new FreelancerProfileStubConfiguration()); // quitar al unir HU-06
        modelBuilder.ApplyConfiguration(new ServiceRequestConfiguration());
    }
}

// Solo para "dotnet ef". También se borra al unir ramas.
public class ServiceRequestDbContextFactory : IDesignTimeDbContextFactory<ServiceRequestDbContext>
{
    public ServiceRequestDbContext CreateDbContext(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable("RELAYHR_CONNECTION")
                 ?? ServiceRequestModuleExtensions.DefaultConnection;
        var options = new DbContextOptionsBuilder<ServiceRequestDbContext>().UseSqlServer(cs).Options;
        return new ServiceRequestDbContext(options);
    }
}