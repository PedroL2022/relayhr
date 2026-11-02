using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RelayHR.Web.Entities;
using RelayHR.Web.Security;

namespace RelayHR.Web.Data;

/// <summary>
/// Datos de demostración para desarrollo: así la aplicación se puede probar completa apenas arranca.
/// Solo se ejecuta en el entorno Development y únicamente si la base de datos está vacía.
/// </summary>
public static class DataSeeder
{
    public const string AdminEmail = "admin@relayhr.com";
    public const string RecruiterEmail = "reclutador@relayhr.com";
    public const string FreelancerEmail = "freelancer@relayhr.com";

    public const string AdminPassword = "Admin123!";
    public const string RecruiterPassword = "Reclutador123!";
    public const string FreelancerPassword = "Freelancer123!";

    public static async Task SeedAsync(AppDbContext db, IPasswordHasher<User> hasher, TimeProvider time)
    {
        if (await db.Users.AnyAsync() || await db.Departments.AnyAsync())
        {
            return;
        }

        var now = time.GetUtcNow().UtcDateTime;

        // ---- Departamentos y puestos (uno completo y dos incompletos, para ver los campos "por completar") ----
        var tech = new Department { Name = "Tecnología" };
        var hr = new Department { Name = "Recursos Humanos" };
        var finance = new Department { Name = "Finanzas" };
        var operations = new Department { Name = "Operaciones" };
        db.Departments.AddRange(tech, hr, finance, operations);

        var backendDev = new Position
        {
            Title = "Desarrollador Backend",
            Department = tech,
            Description = "Diseño y mantenimiento de APIs y servicios internos.",
            Requirements = "3 años de experiencia con C# y SQL Server."
        };
        var financialAnalyst = new Position
        {
            Title = "Analista Financiero",
            Department = finance,
            Description = "Seguimiento de presupuestos y reportes mensuales.",
            Requirements = null
        };
        var assistant = new Position
        {
            Title = "Asistente Administrativo",
            Department = null,
            Description = null,
            Requirements = null
        };
        db.Positions.AddRange(backendDev, financialAnalyst, assistant);

        // ---- Empleados ----
        db.Employees.AddRange(
            new Employee { FullName = "Ana Martínez", Position = backendDev, Department = tech },
            new Employee { FullName = "Luis Pérez", Position = financialAnalyst, Department = finance },
            new Employee { FullName = "Carla Rodríguez", Position = assistant, Department = operations },
            new Employee { FullName = "Miguel Santos", Position = backendDev, Department = tech },
            new Employee { FullName = "Rosa Jiménez", Position = null, Department = hr });

        // ---- Usuarios de demostración ----
        var admin = NewUser(hasher, "Administrador RRHH", AdminEmail, AdminPassword, Roles.HRAdmin, "Demo S.R.L.", now);
        var recruiter = NewUser(hasher, "Reclutador Demo", RecruiterEmail, RecruiterPassword, Roles.Recruiter, "Demo S.R.L.", now);
        var freelancer = NewUser(hasher, "Daniela Freelancer", FreelancerEmail, FreelancerPassword, Roles.Freelancer, null, now);
        var designer = NewUser(hasher, "Pedro Diseñador", "pedro@relayhr.com", FreelancerPassword, Roles.Freelancer, null, now);
        var accountant = NewUser(hasher, "Sofía Contadora", "sofia@relayhr.com", FreelancerPassword, Roles.Freelancer, null, now);
        db.Users.AddRange(admin, recruiter, freelancer, designer, accountant);

        // ---- Perfiles de trabajadores independientes ----
        var profile1 = new FreelancerProfile { User = freelancer };
        profile1.Apply("Desarrollo de software", 5,
            "Desarrolladora full stack con experiencia en C#, SQL Server y React.",
            "https://portafolio.example.com/daniela", 1500m, true, "Lunes a viernes por la tarde", now);

        var profile2 = new FreelancerProfile { User = designer };
        profile2.Apply("Diseño gráfico y UX", 3,
            "Diseño de interfaces, identidad visual y prototipos.",
            null, 900m, true, null, now);

        var profile3 = new FreelancerProfile { User = accountant };
        profile3.Apply("Contabilidad y finanzas", 8,
            "Cierres contables, conciliaciones y reportes para pequeñas empresas.",
            null, 1200m, false, "Disponible a partir del próximo mes", now);

        db.FreelancerProfiles.AddRange(profile1, profile2, profile3);

        await db.SaveChangesAsync();
    }

    private static User NewUser(
        IPasswordHasher<User> hasher, string fullName, string email, string password,
        string role, string? company, DateTime now)
    {
        var user = new User
        {
            FullName = fullName,
            Email = email,
            Role = role,
            CompanyName = company,
            CreatedAt = now
        };
        user.PasswordHash = hasher.HashPassword(user, password);
        return user;
    }
}
