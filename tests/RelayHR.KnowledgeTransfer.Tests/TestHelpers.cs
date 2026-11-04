using Microsoft.EntityFrameworkCore;
using RelayHR.Web.Data.KnowledgeTransfer;
using RelayHR.Web.Entities.Stubs;

namespace RelayHR.KnowledgeTransfer.Tests;

internal static class TestHelpers
{
    // Cada prueba usa su propia base en memoria, así no se afectan entre sí.
    public static KnowledgeTransferDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<KnowledgeTransferDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new KnowledgeTransferDbContext(options);
    }

    public static async Task<EmployeeExit> AddExitAsync(KnowledgeTransferDbContext db, int employeeId = 1)
    {
        var exit = new EmployeeExit { EmployeeId = employeeId };
        db.EmployeeExits.Add(exit);
        await db.SaveChangesAsync();
        return exit;
    }

    public static async Task<AppUser> AddUserAsync(KnowledgeTransferDbContext db, bool isActive = true)
    {
        var user = new AppUser { IsActive = isActive };
        db.AppUsers.Add(user);
        await db.SaveChangesAsync();
        return user;
    }
}
