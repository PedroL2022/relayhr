using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RelayHR.Web.Entities;

namespace RelayHR.Web.Data.Configurations;

public class FreelancerProfileConfiguration : IEntityTypeConfiguration<FreelancerProfile>
{
    public void Configure(EntityTypeBuilder<FreelancerProfile> builder)
    {
        builder.ToTable("FreelancerProfiles");
        builder.HasKey(x => x.FreelancerProfileId);

        builder.Property(x => x.Category).IsRequired().HasMaxLength(100);
        builder.Property(x => x.ExperienceSummary).IsRequired().HasMaxLength(2000);
        builder.Property(x => x.PortfolioUrl).HasMaxLength(300);
        builder.Property(x => x.HourlyRate).HasColumnType("decimal(18,2)");
        builder.Property(x => x.AvailabilityNote).HasMaxLength(200);

        builder.HasOne(x => x.User).WithOne().HasForeignKey<FreelancerProfile>(x => x.UserId).IsRequired().OnDelete(DeleteBehavior.Restrict);

        // Una relación 1 a 0..1: el índice único lo garantiza en la base de datos.
        builder.HasIndex(x => x.UserId).IsUnique();
    }
}
