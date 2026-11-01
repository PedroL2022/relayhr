using Microsoft.EntityFrameworkCore;
using RelayHR.Web.Entities;

namespace RelayHR.Web.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");

            entity.HasKey(user => user.Id);

            entity.Property(user => user.Email)
                .HasMaxLength(254)
                .IsRequired();

            entity.Property(user => user.NormalizedEmail)
                .HasMaxLength(254)
                .IsRequired();

            entity.HasIndex(user => user.NormalizedEmail)
                .IsUnique();

            entity.Property(user => user.PasswordHash)
                .HasMaxLength(512)
                .IsRequired();

            entity.Property(user => user.SecurityStamp)
                .IsRequired();
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");

            entity.HasKey(role => role.Id);

            entity.Property(role => role.Name)
                .HasMaxLength(50)
                .IsRequired();

            entity.HasIndex(role => role.Name)
                .IsUnique();

            entity.HasData(
                new Role { Id = 1, Name = "HRAdmin" },
                new Role { Id = 2, Name = "Recruiter" },
                new Role { Id = 3, Name = "Company" },
                new Role { Id = 4, Name = "Freelancer" }
            );
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("UserRoles");

            entity.HasKey(userRole => new
            {
                userRole.UserId,
                userRole.RoleId
            });

            entity.HasOne(userRole => userRole.User)
                .WithMany(user => user.UserRoles)
                .HasForeignKey(userRole => userRole.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(userRole => userRole.Role)
                .WithMany(role => role.UserRoles)
                .HasForeignKey(userRole => userRole.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}