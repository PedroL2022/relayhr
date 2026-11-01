using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using RelayHR.Web.Entities;
using RelayHR.Web.Services;

namespace RelayHR.Web.Data;

public class RelayHrDbContext(DbContextOptions<RelayHrDbContext> options)
    : IdentityDbContext<User, IdentityRole<int>, int>(options)
{
    public DbSet<Employee> Employees => Set<Employee>();

    public DbSet<Department> Departments => Set<Department>();

    public DbSet<EmployeeExit> EmployeeExits => Set<EmployeeExit>();

    public DbSet<ExitChecklist> ExitChecklists => Set<ExitChecklist>();

    public DbSet<ChecklistTask> ChecklistTasks => Set<ChecklistTask>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        var isSqlite = Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite";

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.ToTable("Employees");
            entity.HasKey(employee => employee.Id);
            entity.Property(employee => employee.Name).HasMaxLength(150).IsRequired();
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(user => user.Id);
            entity.Property(user => user.Name).HasMaxLength(150).IsRequired();
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.ToTable("Departments");
            entity.HasKey(department => department.Id);
            entity.Property(department => department.Name).HasMaxLength(150).IsRequired();
            entity.HasIndex(department => department.Name).IsUnique();
            entity.HasData(
                new Department { Id = 1, Name = "Tecnología" },
                new Department { Id = 2, Name = "Recursos Humanos" },
                new Department { Id = 3, Name = "Finanzas" });
        });

        modelBuilder.Entity<IdentityRole<int>>().HasData(
            new IdentityRole<int>
            {
                Id = 1,
                Name = IdentityRoles.HumanResources,
                NormalizedName = IdentityRoles.HumanResources.ToUpperInvariant(),
                ConcurrencyStamp = "b6b85188-3fce-4f4b-b34d-1c96a21de139"
            });

        modelBuilder.Entity<EmployeeExit>(entity =>
        {
            entity.ToTable("EmployeeExits", table =>
            {
                table.HasCheckConstraint(
                    "CK_EmployeeExits_Status_NotEmpty",
                    isSqlite
                        ? "length(trim([Status])) > 0"
                        : "LEN(LTRIM(RTRIM([Status]))) > 0");
                table.HasCheckConstraint(
                    "CK_EmployeeExits_Status",
                    isSqlite
                        ? "[Status] IN ('Pending', 'InProgress', 'Completed', 'Cancelled', 'Closed')"
                        : "[Status] IN (N'Pending', N'InProgress', N'Completed', N'Cancelled', N'Closed')");
            });
            entity.HasKey(exit => exit.Id);
            entity.Property(exit => exit.Status).HasMaxLength(50).IsRequired();
            entity.Property(exit => exit.CreatedAt).HasDefaultValueSql(
                isSqlite ? "CURRENT_TIMESTAMP" : "SYSUTCDATETIME()");
            entity.HasIndex(exit => exit.EmployeeId)
                .IsUnique()
                .HasFilter(isSqlite
                    ? "[Status] IN ('Pending', 'InProgress')"
                    : "[Status] IN (N'Pending', N'InProgress')");

            entity.HasOne(exit => exit.Employee)
                .WithMany(employee => employee.Exits)
                .HasForeignKey(exit => exit.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(exit => exit.Checklist)
                .WithOne(checklist => checklist.EmployeeExit)
                .HasForeignKey<ExitChecklist>(checklist => checklist.EmployeeExitId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(exit => exit.ClosedByUser)
                .WithMany()
                .HasForeignKey(exit => exit.ClosedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ExitChecklist>(entity =>
        {
            entity.ToTable("ExitChecklists");
            entity.HasKey(checklist => checklist.Id);
            entity.Property(checklist => checklist.CreatedAt).HasDefaultValueSql(
                isSqlite ? "CURRENT_TIMESTAMP" : "SYSUTCDATETIME()");
        });

        modelBuilder.Entity<ChecklistTask>(entity =>
        {
            entity.ToTable("ChecklistTasks", table =>
                table.HasCheckConstraint(
                    "CK_ChecklistTasks_Status",
                    isSqlite
                        ? "[Status] IN ('Pending', 'InProgress', 'Completed')"
                        : "[Status] IN (N'Pending', N'InProgress', N'Completed')"));
            entity.HasKey(task => task.Id);
            entity.Property(task => task.Title).HasMaxLength(200).IsRequired();
            entity.Property(task => task.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(ChecklistTaskStatus.Pending);
            entity.HasIndex(task => new { task.Status, task.DueDate });

            entity.HasOne(task => task.ExitChecklist)
                .WithMany(checklist => checklist.Tasks)
                .HasForeignKey(task => task.ExitChecklistId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(task => task.Department)
                .WithMany(department => department.ChecklistTasks)
                .HasForeignKey(task => task.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(task => task.ResponsibleUser)
                .WithMany(user => user.AssignedTasks)
                .HasForeignKey(task => task.ResponsibleUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(task => task.Notes).HasMaxLength(500);

            entity.HasOne(task => task.CompletedByUser)
                .WithMany()
                .HasForeignKey(task => task.CompletedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}