using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RelayHR.Web.Entities;

namespace RelayHR.Web.Data.Configurations;

public class ExpressVacancyRequestConfiguration : IEntityTypeConfiguration<ExpressVacancyRequest>
{
    public void Configure(EntityTypeBuilder<ExpressVacancyRequest> builder)
    {
        builder.ToTable("ExpressVacancyRequests", t =>
        {
            t.HasCheckConstraint(
                "CK_ExpressVacancyRequests_Status",
                "[Status] IN ('Pending', 'Processing', 'Completed', 'Failed')");
        });

        builder.HasKey(r => r.ExpressVacancyRequestId);

        builder.Property(r => r.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();

        builder.Property(r => r.NextAttemptAt).HasColumnType("datetime2");
        builder.Property(r => r.LockedUntil).HasColumnType("datetime2");
        builder.Property(r => r.LastAttemptAt).HasColumnType("datetime2");
        builder.Property(r => r.CreatedAt).HasColumnType("datetime2");
        builder.Property(r => r.CompletedAt).HasColumnType("datetime2");

        builder.Property(r => r.LastError).HasMaxLength(2000);

        // Una salida solo puede tener una solicitud: evita duplicados desde el origen.
        builder.HasOne(r => r.EmployeeExit)
            .WithOne()
            .HasForeignKey<ExpressVacancyRequest>(r => r.EmployeeExitId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.EmployeeExitId)
            .IsUnique()
            .HasDatabaseName("IX_ExpressVacancyRequests_EmployeeExitId");

        // Índice para la consulta del proceso en segundo plano (solicitudes pendientes por fecha).
        builder.HasIndex(r => new { r.Status, r.NextAttemptAt })
            .HasDatabaseName("IX_ExpressVacancyRequests_Status_NextAttemptAt");
    }
}