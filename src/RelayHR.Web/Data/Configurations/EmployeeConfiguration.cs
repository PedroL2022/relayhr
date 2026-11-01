using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RelayHR.Web.Entities;
namespace RelayHR.Web.Data.Configurations;
public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).IsRequired().HasMaxLength(150);
        builder.HasOne(e => e.Position).WithMany().HasForeignKey(e => e.PositionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Department).WithMany().HasForeignKey(e => e.DepartmentId).OnDelete(DeleteBehavior.Restrict);
    }
}
