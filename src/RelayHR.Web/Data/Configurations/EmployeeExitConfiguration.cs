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

        builder.Property(x => x.ExitDate)
            .HasColumnType("date");

        builder.Property(x => x.Reason).HasMaxLength(500);

        builder.Property(x => x.CreatedAt)
            .HasColumnType("datetime2");

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
