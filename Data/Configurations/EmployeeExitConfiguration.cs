using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RelayHR.Web.Entities;

namespace RelayHR.Web.Data.Configurations;

public class EmployeeExitConfiguration : IEntityTypeConfiguration<EmployeeExit>
{
    public void Configure(EntityTypeBuilder<EmployeeExit> builder)
    {
        builder.ToTable("EmployeeExits");
        builder.HasKey(x => x.EmployeeExitId);

        builder.Property(x => x.Reason).HasMaxLength(500);
        builder.Property(x => x.Status).IsRequired().HasMaxLength(20).HasConversion<string>();
        builder.Property(x => x.RegisteredBy).IsRequired().HasMaxLength(150);
        builder.Property(x => x.ClosedBy).HasMaxLength(150);

        builder.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).IsRequired().OnDelete(DeleteBehavior.Restrict);
    }
}
