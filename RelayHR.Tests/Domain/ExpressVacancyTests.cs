using RelayHR.Web.Entities;

namespace RelayHR.Tests.Domain;

/// <summary>HU-04 (generación) y HU-05 (revisión y publicación).</summary>
public class ExpressVacancyTests
{
    private static EmployeeExit NewExit(int? employeeDepartmentId = null) => new()
    {
        EmployeeExitId = 5,
        CreatedAt = new DateTime(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc),
        Employee = new Employee { EmployeeId = 1, FullName = "Ana Pérez", DepartmentId = employeeDepartmentId }
    };

    private static Position NewPosition(string? description = "Desarrollo de APIs", string? requirements = "3 años", int? departmentId = 3) => new()
    {
        PositionId = 7,
        Title = "Desarrollador Backend",
        Description = description,
        Requirements = requirements,
        DepartmentId = departmentId
    };

    [Fact]
    public void CreateFromExit_CopiesThePositionAndStartsPendingReview()
    {
        var vacancy = ExpressVacancy.CreateFromExit(NewExit(), NewPosition(), DateTime.UtcNow);

        Assert.Equal(VacancyStatus.PendingReview, vacancy.Status);
        Assert.Equal("Desarrollador Backend", vacancy.JobTitle);
        Assert.Equal("Desarrollo de APIs", vacancy.JobDescription);
        Assert.Equal("3 años", vacancy.Requirements);
        Assert.Equal(3, vacancy.DepartmentId);
        Assert.Equal(7, vacancy.PositionId);
        Assert.Empty(vacancy.GetMissingFields());
    }

    [Fact]
    public void CreateFromExit_IsLinkedToTheExitThatOriginatedIt()
    {
        var exit = NewExit();

        var vacancy = ExpressVacancy.CreateFromExit(exit, NewPosition(), DateTime.UtcNow);

        Assert.Same(exit, vacancy.EmployeeExit);
    }

    [Fact]
    public void CreateFromExit_WithoutRequirements_MarksOnlyThatFieldAsMissing()
    {
        var vacancy = ExpressVacancy.CreateFromExit(NewExit(), NewPosition(requirements: null), DateTime.UtcNow);

        Assert.Equal(new[] { "Requisitos" }, vacancy.GetMissingFields());
        Assert.Equal("Desarrollador Backend", vacancy.JobTitle);
    }

    [Fact]
    public void CreateFromExit_BlankTextsCountAsMissing()
    {
        var vacancy = ExpressVacancy.CreateFromExit(NewExit(), NewPosition(description: "   ", requirements: ""), DateTime.UtcNow);

        Assert.Contains("Descripción", vacancy.GetMissingFields());
        Assert.Contains("Requisitos", vacancy.GetMissingFields());
    }

    [Fact]
    public void CreateFromExit_WithoutPosition_StillCreatesVacancyWithEverythingMissing()
    {
        var vacancy = ExpressVacancy.CreateFromExit(NewExit(), null, DateTime.UtcNow);

        Assert.Equal(VacancyStatus.PendingReview, vacancy.Status);
        Assert.Equal(4, vacancy.GetMissingFields().Count);
        Assert.Null(vacancy.PositionId);
    }

    [Fact]
    public void CreateFromExit_PositionWithoutDepartment_UsesTheEmployeesDepartment()
    {
        var vacancy = ExpressVacancy.CreateFromExit(NewExit(employeeDepartmentId: 9), NewPosition(departmentId: null), DateTime.UtcNow);

        Assert.Equal(9, vacancy.DepartmentId);
    }

    [Fact]
    public void Edit_MovesFromPendingReviewToInReview_AndFillsMissingFields()
    {
        var vacancy = ExpressVacancy.CreateFromExit(NewExit(), NewPosition(requirements: null), DateTime.UtcNow);

        vacancy.Edit("Desarrollador Backend Sr.", 3, "Desarrollo de APIs", "5 años de experiencia");

        Assert.Equal(VacancyStatus.InReview, vacancy.Status);
        Assert.Empty(vacancy.GetMissingFields());
        Assert.Equal("5 años de experiencia", vacancy.Requirements);
    }

    [Fact]
    public void TryPublish_WithMissingFields_IsBlockedAndSaysWhatIsMissing()
    {
        var vacancy = ExpressVacancy.CreateFromExit(NewExit(), NewPosition(requirements: null), DateTime.UtcNow);

        var published = vacancy.TryPublish("Reclutador", DateTime.UtcNow, out var missing);

        Assert.False(published);
        Assert.Equal(new[] { "Requisitos" }, missing);
        Assert.NotEqual(VacancyStatus.Published, vacancy.Status);
        Assert.Null(vacancy.PublishedAt);
    }

    [Fact]
    public void TryPublish_WhenComplete_PublishesAndRecordsDateTimeAndUser()
    {
        var vacancy = ExpressVacancy.CreateFromExit(NewExit(), NewPosition(), DateTime.UtcNow);
        var now = new DateTime(2026, 11, 3, 8, 0, 0, DateTimeKind.Utc);

        var published = vacancy.TryPublish("Reclutador Demo", now, out var missing);

        Assert.True(published);
        Assert.Empty(missing);
        Assert.Equal(VacancyStatus.Published, vacancy.Status);
        Assert.Equal(now, vacancy.PublishedAt);
        Assert.Equal("Reclutador Demo", vacancy.PublishedBy);
    }

    [Fact]
    public void TryPublish_IsNotPossibleTwice_AndPublishedVacancyCannotBeEdited()
    {
        var vacancy = ExpressVacancy.CreateFromExit(NewExit(), NewPosition(), DateTime.UtcNow);
        Assert.True(vacancy.TryPublish("x", DateTime.UtcNow, out _));

        Assert.False(vacancy.TryPublish("x", DateTime.UtcNow, out _));
        Assert.False(vacancy.CanEdit);
    }

    [Fact]
    public void TimeToPublish_MeasuresFromExitRegistration_AndMeetsTheGoalUnder48Hours()
    {
        var exit = NewExit(); // registrada 2026-11-02 12:00 UTC
        var vacancy = ExpressVacancy.CreateFromExit(exit, NewPosition(), exit.CreatedAt);

        vacancy.TryPublish("x", exit.CreatedAt.AddHours(47), out _);

        Assert.Equal(TimeSpan.FromHours(47), vacancy.TimeToPublish);
        Assert.True(vacancy.MetPublishGoal);
    }

    [Fact]
    public void MetPublishGoal_IsFalseWhenPublishedAfter48Hours()
    {
        var exit = NewExit();
        var vacancy = ExpressVacancy.CreateFromExit(exit, NewPosition(), exit.CreatedAt);

        vacancy.TryPublish("x", exit.CreatedAt.AddHours(49), out _);

        Assert.False(vacancy.MetPublishGoal);
    }

    [Fact]
    public void TimeToPublish_IsNullWhileNotPublished()
    {
        var vacancy = ExpressVacancy.CreateFromExit(NewExit(), NewPosition(), DateTime.UtcNow);

        Assert.Null(vacancy.TimeToPublish);
        Assert.False(vacancy.MetPublishGoal);
    }
}
