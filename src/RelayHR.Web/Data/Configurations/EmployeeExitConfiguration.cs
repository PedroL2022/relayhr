using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RelayHR.Web.Entities;
namespace RelayHR.Web.Data.Configurations;
public class EmployeeExitConfiguration : IEntityTypeConfiguration<EmployeeExit>
{
    public void Configure(EntityTypeBuilder<EmployeeExit> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.EffectiveDate).HasColumnType("date");
    }
}
