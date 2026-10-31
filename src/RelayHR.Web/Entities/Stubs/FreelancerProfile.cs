using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RelayHR.Web.Entities;

// ============================================================================
// STUB TEMPORAL de la HU-06. BORRAR ESTE ARCHIVO COMPLETO cuando la rama de
// FreelancerProfile llegue a main (la entidad real debe tener Id, UserId, IsActive).
// ============================================================================
public class FreelancerProfile
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public bool IsActive { get; set; } = true;
}

public class FreelancerProfileStubConfiguration : IEntityTypeConfiguration<FreelancerProfile>
{
    public void Configure(EntityTypeBuilder<FreelancerProfile> builder)
    {
        builder.ToTable("FreelancerProfiles");
        builder.HasKey(p => p.Id);
        builder.HasIndex(p => p.UserId);
    }
}