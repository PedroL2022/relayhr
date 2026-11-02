using Microsoft.EntityFrameworkCore;
using RelayHR.Web.Data;
using RelayHR.Web.Entities;
using RelayHR.Web.Security;

namespace RelayHR.Tests.Support;

/// <summary>Reloj controlable para probar fechas sin esperar.</summary>
public class TestClock : TimeProvider
{
    public DateTimeOffset Now { get; set; } = new(2026, 11, 2, 15, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => Now;
}

public static class TestDb
{
    /// <summary>Base de datos en memoria, distinta para cada prueba.</summary>
    public static AppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    /// <summary>Crea un empleado con un puesto completo (o incompleto, según se indique).</summary>
    public static async Task<Employee> AddEmployeeAsync(
        AppDbContext db, string name = "Ana Pérez", bool completePosition = true)
    {
        var department = new Department { Name = "Tecnología" };
        var position = new Position
        {
            Title = "Desarrollador Backend",
            Department = department,
            Description = completePosition ? "Desarrollo de APIs" : null,
            Requirements = completePosition ? "3 años de experiencia" : null
        };
        var employee = new Employee { FullName = name, Position = position, Department = department };

        db.Employees.Add(employee);
        await db.SaveChangesAsync();
        return employee;
    }

    public static async Task<User> AddUserAsync(
        AppDbContext db, string role, string name = "Usuario", string? company = null)
    {
        var user = new User
        {
            FullName = name,
            Email = $"{Guid.NewGuid():N}@prueba.com",
            PasswordHash = "hash",
            Role = role,
            CompanyName = company,
            CreatedAt = DateTime.UtcNow
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    public static async Task<FreelancerProfile> AddProfileAsync(
        AppDbContext db, User user, string category = "Desarrollo de software",
        decimal rate = 1000m, bool available = true)
    {
        var profile = new FreelancerProfile { UserId = user.UserId };
        profile.Apply(category, 4, "Experiencia de prueba", null, rate, available, null, DateTime.UtcNow);

        db.FreelancerProfiles.Add(profile);
        await db.SaveChangesAsync();
        return profile;
    }
}
