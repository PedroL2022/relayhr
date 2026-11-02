using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RelayHR.Web.Entities;

namespace RelayHR.Web.Data.Configurations;

public class ExitTaskConfiguration : IEntityTypeConfiguration<ExitTask>
{
    public void Configure(EntityTypeBuilder<ExitTask> builder)
    {
        builder.ToTable("ExitTasks");
        builder.HasKey(x => x.ExitTaskId);

        builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Area).IsRequired().HasMaxLength(30).HasConversion<string>();
        builder.Property(x => x.ResponsibleName).IsRequired().HasMaxLength(150);
        builder.Property(x => x.Status).IsRequired().HasMaxLength(20).HasConversion<string>();
        builder.Property(x => x.Kind).IsRequired().HasMaxLength(30).HasConversion<string>();
        builder.Property(x => x.CompletedBy).HasMaxLength(150);

        builder.HasOne(x => x.EmployeeExit).WithMany(e => e.Tasks).HasForeignKey(x => x.EmployeeExitId).IsRequired().OnDelete(DeleteBehavior.Restrict);
    }
}
