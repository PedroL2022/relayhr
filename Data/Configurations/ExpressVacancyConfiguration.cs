using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RelayHR.Web.Entities;

namespace RelayHR.Web.Data.Configurations;

public class ExpressVacancyConfiguration : IEntityTypeConfiguration<ExpressVacancy>
{
    public void Configure(EntityTypeBuilder<ExpressVacancy> builder)
    {
        builder.ToTable("ExpressVacancies");
        builder.HasKey(x => x.ExpressVacancyId);

        builder.Property(x => x.JobTitle).HasMaxLength(150);
        builder.Property(x => x.JobDescription).HasMaxLength(4000);
        builder.Property(x => x.Requirements).HasMaxLength(4000);
        builder.Property(x => x.Status).IsRequired().HasMaxLength(20).HasConversion<string>();
        builder.Property(x => x.PublishedBy).HasMaxLength(150);

        builder.HasOne(x => x.EmployeeExit).WithOne(e => e.ExpressVacancy).HasForeignKey<ExpressVacancy>(x => x.EmployeeExitId).IsRequired().OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Position).WithMany().HasForeignKey(x => x.PositionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);

        // Una relación 1 a 0..1: el índice único lo garantiza en la base de datos.
        builder.HasIndex(x => x.EmployeeExitId).IsUnique();
    }
}
