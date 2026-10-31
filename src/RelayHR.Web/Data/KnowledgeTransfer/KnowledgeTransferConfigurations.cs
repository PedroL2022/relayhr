using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RelayHR.Web.Entities.KnowledgeTransfer;
using RelayHR.Web.Entities.Stubs;

namespace RelayHR.Web.Data.KnowledgeTransfer;

// TEMPORAL: configuración del stub. Borrar cuando exista la configuración real de EmployeeExit.
public class EmployeeExitStubConfiguration : IEntityTypeConfiguration<EmployeeExit>
{
    public void Configure(EntityTypeBuilder<EmployeeExit> builder)
    {
        builder.ToTable("EmployeeExits");
        builder.HasKey(e => e.Id);
    }
}

public class KnowledgeTransferProjectConfiguration : IEntityTypeConfiguration<KnowledgeTransferProject>
{
    public void Configure(EntityTypeBuilder<KnowledgeTransferProject> builder)
    {
        builder.ToTable("KnowledgeTransferProjects");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Description).IsRequired().HasMaxLength(2000);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(p => p.CreatedAt).IsRequired();

        builder.HasOne(p => p.EmployeeExit)
            .WithMany()
            .HasForeignKey(p => p.EmployeeExitId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.EmployeeExitId);
    }
}

public class KnowledgeTransferTaskConfiguration : IEntityTypeConfiguration<KnowledgeTransferTask>
{
    public void Configure(EntityTypeBuilder<KnowledgeTransferTask> builder)
    {
        builder.ToTable("KnowledgeTransferTasks");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title).IsRequired().HasMaxLength(200);
        builder.Property(t => t.Description).IsRequired().HasMaxLength(2000);
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(t => t.CreatedAt).IsRequired();

        builder.HasOne(t => t.EmployeeExit)
            .WithMany()
            .HasForeignKey(t => t.EmployeeExitId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict evita el error de "múltiples rutas de cascada" en SQL Server.
        builder.HasOne(t => t.Project)
            .WithMany(p => p.Tasks)
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => t.EmployeeExitId);
        builder.HasIndex(t => t.ProjectId);
    }
}
