using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RelayHR.Web.Entities;

namespace RelayHR.Web.Data.Configurations;

public class ServiceRequestConfiguration : IEntityTypeConfiguration<ServiceRequest>
{
    public void Configure(EntityTypeBuilder<ServiceRequest> builder)
    {
        builder.ToTable("ServiceRequests");
        builder.HasKey(x => x.ServiceRequestId);

        builder.Property(x => x.CompanyName).IsRequired().HasMaxLength(150);
        builder.Property(x => x.Status).IsRequired().HasMaxLength(20).HasConversion<string>();

        builder.HasOne(x => x.FreelancerProfile).WithMany().HasForeignKey(x => x.FreelancerProfileId).IsRequired().OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.RequestedBy).WithMany().HasForeignKey(x => x.RequestedByUserId).IsRequired().OnDelete(DeleteBehavior.Restrict);
    }
}
