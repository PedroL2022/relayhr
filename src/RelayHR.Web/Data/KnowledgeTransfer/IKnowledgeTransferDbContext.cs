using Microsoft.EntityFrameworkCore;
using RelayHR.Web.Entities.KnowledgeTransfer;
using RelayHR.Web.Entities.Stubs;

namespace RelayHR.Web.Data.KnowledgeTransfer;

public interface IKnowledgeTransferDbContext
{
    DbSet<EmployeeExit> EmployeeExits { get; }
    DbSet<AppUser> AppUsers { get; }
    DbSet<KnowledgeTransferProject> KnowledgeTransferProjects { get; }
    DbSet<KnowledgeTransferTask> KnowledgeTransferTasks { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
