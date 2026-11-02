using RelayHR.Web.Data;
using RelayHR.Web.Repository;
using RelayHR.Web.Services;

namespace RelayHR.Tests.Support;

/// <summary>Arma los servicios con sus repositorios reales sobre una base de datos de prueba.</summary>
public static class Build
{
    public static ExitService Exit(AppDbContext db, TimeProvider clock) => new(
        new EmployeeRepository(db),
        new ExitRepository(db),
        new ExitTaskRepository(db),
        new KnowledgeItemRepository(db),
        new VacancyRepository(db),
        new UnitOfWork(db),
        clock);

    public static VacancyService Vacancy(AppDbContext db, TimeProvider clock) => new(
        new VacancyRepository(db),
        new DepartmentRepository(db),
        new UnitOfWork(db),
        clock);

    public static TalentService Talent(AppDbContext db, TimeProvider clock) => new(
        new FreelancerRepository(db),
        new ServiceRequestRepository(db),
        new UserRepository(db),
        new UnitOfWork(db),
        clock);
}
