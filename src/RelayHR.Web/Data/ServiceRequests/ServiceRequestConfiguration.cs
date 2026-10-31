using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RelayHR.Web.Entities;

namespace RelayHR.Web.Data.ServiceRequests;

public class ServiceRequestConfiguration : IEntityTypeConfiguration<ServiceRequest>
{
    // Las fechas se leen siempre con Kind = Utc (así el JSON sale con "Z").
    private static readonly ValueConverter<DateTime, DateTime> UtcConverter =
        new(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

    public void Configure(EntityTypeBuilder<ServiceRequest> builder)
    {
        builder.ToTable("ServiceRequests", t =>
            t.HasCheckConstraint("CK_ServiceRequests_Status",
                "[Status] IN ('Sent','Accepted','Rejected','Confirmed')"));

        builder.HasKey(r => r.ServiceRequestId);

        builder.Property(r => r.CompanyId).IsRequired();
        builder.Property(r => r.Description).IsRequired().HasMaxLength(1000);
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(r => r.CreatedAt).HasConversion(UtcConverter).IsRequired();
        builder.Property(r => r.RespondedAt).HasConversion(UtcConverter);
        builder.Property(r => r.ConfirmedAt).HasConversion(UtcConverter);

        builder.HasOne(r => r.FreelancerProfile)
               .WithMany()
               .HasForeignKey(r => r.FreelancerProfileId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.CompanyId);
        builder.HasIndex(r => r.FreelancerProfileId);
        builder.HasIndex(r => r.Status);

        // Protección de BD contra duplicados (clics repetidos / carreras):
        // una sola solicitud activa (Sent o Accepted) por empresa y profesional.
        builder.HasIndex(r => new { r.CompanyId, r.FreelancerProfileId })
               .IsUnique()
               .HasFilter("[Status] IN ('Sent','Accepted')")
               .HasDatabaseName("UX_ServiceRequests_ActivePerCompanyFreelancer");
    }
}