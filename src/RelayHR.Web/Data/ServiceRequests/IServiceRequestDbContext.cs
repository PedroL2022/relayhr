using Microsoft.EntityFrameworkCore;
using RelayHR.Web.Entities;

namespace RelayHR.Web.Data.ServiceRequests;

/// <summary>
/// Contrato mínimo que necesita el módulo HU-07. El DbContext principal del proyecto
/// solo debe implementar esta interfaz (2 DbSet) cuando se unan las ramas.
/// </summary>
public interface IServiceRequestDbContext
{
    DbSet<ServiceRequest> ServiceRequests { get; }
    DbSet<FreelancerProfile> FreelancerProfiles { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}