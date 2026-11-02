using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RelayHR.Web.Entities;

namespace RelayHR.Web.Data.Configurations;

public class KnowledgeItemConfiguration : IEntityTypeConfiguration<KnowledgeItem>
{
    public void Configure(EntityTypeBuilder<KnowledgeItem> builder)
    {
        builder.ToTable("KnowledgeItems");
        builder.HasKey(x => x.KnowledgeItemId);

        builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Description).IsRequired().HasMaxLength(2000);
        builder.Property(x => x.CurrentStatus).IsRequired().HasMaxLength(200);

        builder.HasOne(x => x.EmployeeExit).WithMany(e => e.KnowledgeItems).HasForeignKey(x => x.EmployeeExitId).IsRequired().OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.AssignedTo).WithMany().HasForeignKey(x => x.AssignedToEmployeeId).OnDelete(DeleteBehavior.Restrict);
    }
}
