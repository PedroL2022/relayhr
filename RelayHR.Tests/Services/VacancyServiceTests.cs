using Microsoft.EntityFrameworkCore;
using RelayHR.Tests.Support;
using RelayHR.Web.Data;
using RelayHR.Web.Entities;
using RelayHR.Web.Services;

namespace RelayHR.Tests.Services;

/// <summary>Revisión y publicación de la Vacante Express (HU-05).</summary>
public class VacancyServiceTests
{
    private static async Task<(AppDbContext Db, VacancyService Service, TestClock Clock, int VacancyId)> SetupAsync(bool completePosition)
    {
        var db = TestDb.Create();
        var clock = new TestClock();
        var employee = await TestDb.AddEmployeeAsync(db, completePosition: completePosition);

        await Build.Exit(db, clock).RegisterAsync(employee.EmployeeId, new DateOnly(2026, 11, 20), null, "Admin");
        var vacancyId = (await db.ExpressVacancies.FirstAsync()).ExpressVacancyId;

        return (db, Build.Vacancy(db, clock), clock, vacancyId);
    }

    [Fact]
    public async Task Update_SavesChangesAndMovesToInReview()
    {
        var (db, service, _, id) = await SetupAsync(completePosition: false);
        var departmentId = (await db.Departments.FirstAsync()).DepartmentId;

        var result = await service.UpdateAsync(id, "Desarrollador Backend Sr.", departmentId, "Desarrollo de APIs", "5 años");

        Assert.True(result.Succeeded);
        var vacancy = await db.ExpressVacancies.FirstAsync();
        Assert.Equal(VacancyStatus.InReview, vacancy.Status);
        Assert.Equal("Desarrollador Backend Sr.", vacancy.JobTitle);
        Assert.Equal("5 años", vacancy.Requirements);
        Assert.Empty(vacancy.GetMissingFields());
    }

    [Fact]
    public async Task Update_WithUnknownDepartment_Fails()
    {
        var (_, service, _, id) = await SetupAsync(completePosition: true);

        var result = await service.UpdateAsync(id, "Título", 9999, "Descripción", "Requisitos");

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Publish_WithMissingFields_IsBlockedAndTheMessageSaysWhatIsMissing()
    {
        var (db, service, _, id) = await SetupAsync(completePosition: false);

        var result = await service.PublishAsync(id, "Reclutador");

        Assert.False(result.Succeeded);
        Assert.Contains("Descripción", result.Error!);
        Assert.Contains("Requisitos", result.Error!);
        Assert.NotEqual(VacancyStatus.Published, (await db.ExpressVacancies.FirstAsync()).Status);
    }

    [Fact]
    public async Task Publish_WhenComplete_RecordsDateTimeAndMeasuresTheTimeSinceTheExit()
    {
        var (db, service, clock, id) = await SetupAsync(completePosition: true);
        clock.Now = clock.Now.AddHours(30);

        var result = await service.PublishAsync(id, "Reclutador Demo");

        Assert.True(result.Succeeded);
        var vacancy = await db.ExpressVacancies.Include(v => v.EmployeeExit).FirstAsync();
        Assert.Equal(VacancyStatus.Published, vacancy.Status);
        Assert.Equal("Reclutador Demo", vacancy.PublishedBy);
        Assert.Equal(clock.Now.UtcDateTime, vacancy.PublishedAt);
        Assert.Equal(TimeSpan.FromHours(30), vacancy.TimeToPublish);
        Assert.True(vacancy.MetPublishGoal);
    }

    [Fact]
    public async Task Publish_AfterMoreThan48Hours_DoesNotMeetTheGoal()
    {
        var (db, service, clock, id) = await SetupAsync(completePosition: true);
        clock.Now = clock.Now.AddHours(72);

        await service.PublishAsync(id, "Reclutador");

        var vacancy = await db.ExpressVacancies.Include(v => v.EmployeeExit).FirstAsync();
        Assert.False(vacancy.MetPublishGoal);
    }

    [Fact]
    public async Task APublishedVacancy_CannotBePublishedOrEditedAgain()
    {
        var (_, service, _, id) = await SetupAsync(completePosition: true);
        await service.PublishAsync(id, "Reclutador");

        Assert.False((await service.PublishAsync(id, "Reclutador")).Succeeded);
        Assert.False((await service.UpdateAsync(id, "Otro título", null, "d", "r")).Succeeded);
    }
}
