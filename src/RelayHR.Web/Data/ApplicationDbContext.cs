using Microsoft.EntityFrameworkCore;
using RelayHR.Web.Models;

namespace RelayHR.Web.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<FreelancerProfile> FreelancerProfiles => Set<FreelancerProfile>();
        public DbSet<FreelancerCategory> FreelancerCategories => Set<FreelancerCategory>();
        public DbSet<FreelancerPortfolio> FreelancerPortfolios => Set<FreelancerPortfolio>();
        public DbSet<FreelancerAvailability> FreelancerAvailabilities => Set<FreelancerAvailability>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. Configuración de FreelancerProfile
            modelBuilder.Entity<FreelancerProfile>(entity =>
            {
                entity.Property(p => p.HourlyRate).HasPrecision(18, 2);
                entity.Property(p => p.Currency).HasDefaultValue("DOP").HasMaxLength(3);

                // Regla: Tarifa >= 0
                entity.ToTable(t => t.HasCheckConstraint("CK_FreelancerProfile_HourlyRate", "[HourlyRate] >= 0"));

                // Relaciones
                entity.HasOne(p => p.User)
                      .WithOne()
                      .HasForeignKey<FreelancerProfile>(p => p.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.Category)
                      .WithMany(c => c.FreelancerProfiles)
                      .HasForeignKey(p => p.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Índices para optimizar el filtrado (< 3 segundos)
                entity.HasIndex(p => new { p.IsActive, p.CategoryId, p.HourlyRate })
                      .HasDatabaseName("IX_FreelancerProfile_Search");
            });

            // 2. Configuración de FreelancerAvailability
            modelBuilder.Entity<FreelancerAvailability>(entity =>
            {
                // Regla: Intervalo de fechas válido
                entity.ToTable(t => t.HasCheckConstraint(
                    "CK_FreelancerAvailability_ValidInterval",
                    "[AvailableUntil] >= [AvailableFrom]"));

                entity.HasOne(a => a.FreelancerProfile)
                      .WithMany(p => p.Availabilities)
                      .HasForeignKey(a => a.FreelancerProfileId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(a => new { a.FreelancerProfileId, a.AvailableFrom, a.AvailableUntil })
                      .HasDatabaseName("IX_FreelancerAvailability_Dates");
            });

            // 3. Configuración de FreelancerPortfolio
            modelBuilder.Entity<FreelancerPortfolio>(entity =>
            {
                entity.HasOne(pf => pf.FreelancerProfile)
                      .WithMany(p => p.Portfolios)
                      .HasForeignKey(pf => pf.FreelancerProfileId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // 4. Datos de prueba (Seed Data)
            modelBuilder.Entity<User>().HasData(
                new User { Id = 1, FullName = "Carlos Pérez", Email = "carlos@ejemplo.com", PasswordHash = "HASH_SECRETO_1" },
                new User { Id = 2, FullName = "Laura Gómez", Email = "laura@ejemplo.com", PasswordHash = "HASH_SECRETO_2" },
                new User { Id = 3, FullName = "Marcos Díaz", Email = "marcos@ejemplo.com", PasswordHash = "HASH_SECRETO_3" }
            );

            modelBuilder.Entity<FreelancerCategory>().HasData(
                new FreelancerCategory { Id = 1, Name = "Diseño UI/UX", IsActive = true },
                new FreelancerCategory { Id = 2, Name = "Contabilidad y Finanzas", IsActive = true },
                new FreelancerCategory { Id = 3, Name = "Desarrollo de software", IsActive = true }
            );

            modelBuilder.Entity<FreelancerProfile>().HasData(
                new FreelancerProfile
                {
                    Id = 25,
                    UserId = 1,
                    CategoryId = 3,
                    ProfessionalTitle = "Desarrollador Backend",
                    Experience = "4 años desarrollando APIs REST con .NET.",
                    HourlyRate = 1000m,
                    Currency = "DOP",
                    IsActive = true
                },
                new FreelancerProfile
                {
                    Id = 26,
                    UserId = 2,
                    CategoryId = 1,
                    ProfessionalTitle = "Diseñadora UX/UI Senior",
                    Experience = "5 años diseñando interfaces web y móviles en Figma.",
                    HourlyRate = 1200m,
                    Currency = "DOP",
                    IsActive = true
                },
                new FreelancerProfile
                {
                    Id = 27,
                    UserId = 3,
                    CategoryId = 3,
                    ProfessionalTitle = "Arquitecto Cloud .NET",
                    Experience = "7 años en microservicios y Azure.",
                    HourlyRate = 2000m,
                    Currency = "DOP",
                    IsActive = false // Perfil inactivo para probar que no aparece en búsquedas
                }
            );

            modelBuilder.Entity<FreelancerAvailability>().HasData(
                new FreelancerAvailability
                {
                    Id = 1,
                    FreelancerProfileId = 25,
                    AvailableFrom = new DateOnly(2026, 11, 10),
                    AvailableUntil = new DateOnly(2026, 11, 30)
                },
                new FreelancerAvailability
                {
                    Id = 2,
                    FreelancerProfileId = 26,
                    AvailableFrom = new DateOnly(2026, 11, 1),
                    AvailableUntil = new DateOnly(2026, 11, 15)
                }
            );

            modelBuilder.Entity<FreelancerPortfolio>().HasData(
                new FreelancerPortfolio
                {
                    Id = 1,
                    FreelancerProfileId = 25,
                    Title = "Sistema de inventario",
                    Description = "Desarrollo de una API para gestión de inventarios.",
                    ProjectUrl = "https://ejemplo.com/proyecto"
                }
            );
        }
    }
}   