using RelayHR.Tests.Support;
using RelayHR.Web.Entities;
using RelayHR.Web.Repository;
using RelayHR.Web.Security;

namespace RelayHR.Tests.Repositories;

/// <summary>Consultas de los repositorios sobre una base de datos de prueba.</summary>
public class RepositoryTests
{
    private static readonly DateOnly ExitDate = new(2026, 11, 20);

    [Fact]
    public async Task EmployeeRepository_GetActive_ExcludesInactiveAndTheIndicatedEmployee()
    {
        var db = TestDb.Create();
        var ana = await TestDb.AddEmployeeAsync(db, "Ana");
        var luis = await TestDb.AddEmployeeAsync(db, "Luis");
        var rosa = await TestDb.AddEmployeeAsync(db, "Rosa");
        await Build.Exit(db, new TestClock()).RegisterAsync(rosa.EmployeeId, ExitDate, null, "Admin");
        var repository = new EmployeeRepository(db);

        var all = await repository.GetActiveAsync();
        var withoutAna = await repository.GetActiveAsync(ana.EmployeeId);

        Assert.Equal(new[] { "Ana", "Luis" }, all.Select(e => e.FullName).ToList());
        Assert.Equal(new[] { "Luis" }, withoutAna.Select(e => e.FullName).ToList());
        Assert.False(await repository.IsActiveAsync(rosa.EmployeeId));
        Assert.True(await repository.IsActiveAsync(luis.EmployeeId));
    }

    [Fact]
    public async Task VacancyRepository_Inbox_FiltersByStatus_AndPublishedAreOnlyThePublishedOnes()
    {
        var db = TestDb.Create();
        var clock = new TestClock();
        var first = await TestDb.AddEmployeeAsync(db, "Ana");
        var second = await TestDb.AddEmployeeAsync(db, "Luis");
        var exits = Build.Exit(db, clock);
        await exits.RegisterAsync(first.EmployeeId, ExitDate, null, "Admin");
        await exits.RegisterAsync(second.EmployeeId, ExitDate, null, "Admin");
        var repository = new VacancyRepository(db);
        var vacancies = Build.Vacancy(db, clock);

        var firstId = (await repository.GetInboxAsync(null)).First().ExpressVacancyId;
        await vacancies.PublishAsync(firstId, "Reclutador");

        Assert.Equal(2, (await repository.GetInboxAsync(null)).Count);
        Assert.Single(await repository.GetInboxAsync(VacancyStatus.PendingReview));
        Assert.Single(await repository.GetInboxAsync(VacancyStatus.Published));
        Assert.Single(await repository.GetPublishedAsync());
        Assert.NotNull(await repository.GetPublishedByIdAsync(firstId));
        Assert.Equal(1, await repository.CountNotPublishedAsync());
    }

    [Fact]
    public async Task VacancyRepository_PublishedById_IsNullForAnUnpublishedVacancy()
    {
        var db = TestDb.Create();
        var employee = await TestDb.AddEmployeeAsync(db);
        await Build.Exit(db, new TestClock()).RegisterAsync(employee.EmployeeId, ExitDate, null, "Admin");
        var repository = new VacancyRepository(db);
        var id = (await repository.GetInboxAsync(null)).Single().ExpressVacancyId;

        Assert.Null(await repository.GetPublishedByIdAsync(id));
    }

    [Fact]
    public async Task KnowledgeItemRepository_ByAssignee_AndUnassignedCount()
    {
        var db = TestDb.Create();
        var leaving = await TestDb.AddEmployeeAsync(db, "Ana");
        var colleague = await TestDb.AddEmployeeAsync(db, "Luis");
        var service = Build.Exit(db, new TestClock());
        var exitId = (await service.RegisterAsync(leaving.EmployeeId, ExitDate, null, "Admin")).Value;
        await service.AddKnowledgeItemAsync(exitId, "Uno", "d", "s", colleague.EmployeeId);
        await service.AddKnowledgeItemAsync(exitId, "Dos", "d", "s", null);
        var repository = new KnowledgeItemRepository(db);

        var assigned = await repository.GetByAssigneeAsync(colleague.EmployeeId);

        Assert.Equal(new[] { "Uno" }, assigned.Select(k => k.Title).ToList());
        Assert.Equal(1, await repository.CountUnassignedAsync());
        Assert.Equal(2, (await repository.GetByExitAsync(exitId)).Count);
    }

    [Fact]
    public async Task ExitRepository_Details_IncludesTasksVacancyAndCountsInProgress()
    {
        var db = TestDb.Create();
        var employee = await TestDb.AddEmployeeAsync(db);
        var exitId = (await Build.Exit(db, new TestClock()).RegisterAsync(employee.EmployeeId, ExitDate, null, "Admin")).Value;
        var repository = new ExitRepository(db);

        var exit = await repository.GetDetailsAsync(exitId);

        Assert.NotNull(exit);
        Assert.Equal(ExitChecklistTemplate.Items.Count, exit!.Tasks.Count);
        Assert.NotNull(exit.ExpressVacancy);
        Assert.Equal(1, await repository.CountInProgressAsync());
        Assert.Null(await repository.GetDetailsAsync(9999));
    }

    [Fact]
    public async Task ServiceRequestRepository_FiltersByRequesterAndByFreelancer()
    {
        var db = TestDb.Create();
        var clock = new TestClock();
        var company = await TestDb.AddUserAsync(db, Roles.HRAdmin, "Admin", "Demo S.R.L.");
        var other = await TestDb.AddUserAsync(db, Roles.Recruiter, "Otro", "Otra S.A.");
        var freelancer = await TestDb.AddUserAsync(db, Roles.Freelancer);
        var profile = await TestDb.AddProfileAsync(db, freelancer);
        var talent = Build.Talent(db, clock);
        await talent.RequestServiceAsync(profile.FreelancerProfileId, company.UserId);
        var repository = new ServiceRequestRepository(db);

        Assert.Single(await repository.GetByRequesterAsync(company.UserId));
        Assert.Empty(await repository.GetByRequesterAsync(other.UserId));
        Assert.Single(await repository.GetByFreelancerUserAsync(freelancer.UserId));
        Assert.True(await repository.HasActiveAsync(profile.FreelancerProfileId, company.UserId));
        Assert.False(await repository.HasActiveAsync(profile.FreelancerProfileId, other.UserId));
        Assert.Equal(1, await repository.CountActiveByRequesterAsync(company.UserId));
        Assert.Equal(1, await repository.CountSentForFreelancerAsync(freelancer.UserId));
    }

    [Fact]
    public async Task UserRepository_FindsByEmail_AndGenericGetByIdWorks()
    {
        var db = TestDb.Create();
        var user = await TestDb.AddUserAsync(db, Roles.HRAdmin, "Admin", "Demo");
        var repository = new UserRepository(db);

        Assert.NotNull(await repository.GetByEmailAsync(user.Email));
        Assert.Null(await repository.GetByEmailAsync("nadie@x.com"));
        Assert.True(await repository.EmailExistsAsync(user.Email));
        Assert.Equal(user.FullName, (await repository.GetByIdAsync(user.UserId))!.FullName);
        Assert.Null(await repository.GetByIdAsync(9999));
    }

    [Fact]
    public async Task UnitOfWork_SavesWhatTheRepositoriesAdded()
    {
        var db = TestDb.Create();
        var departments = new DepartmentRepository(db);
        var unitOfWork = new UnitOfWork(db);

        departments.Add(new Department { Name = "Ventas" });
        departments.Add(new Department { Name = "Contabilidad" });
        Assert.Equal(0, await departments.CountAsync());   // todavía no se guardó

        await unitOfWork.SaveChangesAsync();

        Assert.Equal(2, await departments.CountAsync());
        Assert.Equal(new[] { "Contabilidad", "Ventas" }, (await departments.GetAllOrderedAsync()).Select(d => d.Name).ToList());
    }
}
