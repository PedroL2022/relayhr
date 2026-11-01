using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RelayHR.Web.Entities;

namespace RelayHR.Web.Data.Configurations;

public class ExpressVacancyConfiguration : IEntityTypeConfiguration<ExpressVacancy>
{
    public void Configure(EntityTypeBuilder<ExpressVacancy> builder)
    {
        builder.ToTable("ExpressVacancies", t =>
        {
            // Solo se aceptan los estados definidos en el enum ExpressVacancyStatus.
            t.HasCheckConstraint(
                "CK_ExpressVacancies_Status",
                "[Status] IN ('PendingReview', 'InReview', 'Approved', 'Discarded')");
        });

        builder.HasKey(v => v.ExpressVacancyId);

        // Copia de los datos del puesto: todos opcionales para que la falta de
        // información no impida crear la vacante.
        builder.Property(v => v.JobTitle).HasMaxLength(150);
        builder.Property(v => v.JobDescription).HasMaxLength(4000);
        builder.Property(v => v.Requirements).HasMaxLength(4000);

        // El estado se guarda como texto y por defecto es PendingReview.
        builder.Property(v => v.Status)
            .IsRequired()
            .HasMaxLength(30)
            .HasConversion<string>()
            .HasDefaultValue(ExpressVacancyStatus.PendingReview);

        builder.Property(v => v.CreatedAt)
            .HasColumnType("datetime2")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        // Los campos pendientes se guardan como un arreglo JSON, por ejemplo ["Requirements"].
        var missingFieldsComparer = new ValueComparer<List<string>>(
            (a, b) => a!.SequenceEqual(b!),
            list => list.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
            list => list.ToList());

        builder.Property(v => v.MissingFields)
            .IsRequired()
            .HasColumnType("nvarchar(max)")
            .HasConversion(
                list => JsonSerializer.Serialize(list, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<List<string>>(json, (JsonSerializerOptions?)null) ?? new List<string>(),
                missingFieldsComparer);

        // Relación 1 a 0..1 con la salida: la vacante depende de la salida que la originó.
        builder.HasOne(v => v.EmployeeExit)
            .WithOne(e => e.ExpressVacancy)
            .HasForeignKey<ExpressVacancy>(v => v.EmployeeExitId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        // El índice único impide en la base de datos más de una vacante por salida.
        builder.HasIndex(v => v.EmployeeExitId)
            .IsUnique()
            .HasDatabaseName("IX_ExpressVacancies_EmployeeExitId");

        // Referencia al puesto original; si no existe, queda en null.
        builder.HasOne(v => v.Position)
            .WithMany()
            .HasForeignKey(v => v.PositionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.Department)
            .WithMany()
            .HasForeignKey(v => v.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
