using Microsoft.EntityFrameworkCore;
using RelayHR.Tests.Support;
using RelayHR.Web.Data;
using RelayHR.Web.Entities;
using RelayHR.Web.Security;
using RelayHR.Web.Services;

namespace RelayHR.Tests.Services;

/// <summary>Perfiles de independientes, búsqueda (HU-06) y solicitudes de servicio (HU-07).</summary>
public class TalentServiceTests
{
    private static (AppDbContext Db, TalentService Service) Setup()
    {
        var db = TestDb.Create();
        return (db, Build.Talent(db, new TestClock()));
    }

    // ---------- Perfil ----------

    [Fact]
    public async Task SaveProfile_CreatesTheProfile_AndASecondSaveUpdatesTheSameOne()
    {
        var (db, service) = Setup();
        var freelancer = await TestDb.AddUserAsync(db, Roles.Freelancer, "Daniela");

        var first = await service.SaveProfileAsync(freelancer.UserId, "Desarrollo de software", 5, "Full stack", null, 1500m, true, null);
        var second = await service.SaveProfileAsync(freelancer.UserId, "Desarrollo de software", 6, "Full stack y cloud", "https://portafolio.com", 1800m, false, "Desde enero");

        Assert.True(first.Succeeded);
        Assert.True(second.Succeeded);
        var profile = Assert.Single(await db.FreelancerProfiles.ToListAsync());
        Assert.Equal(6, profile.ExperienceYears);
        Assert.Equal(1800m, profile.HourlyRate);
        Assert.False(profile.IsAvailable);
        Assert.Equal("https://portafolio.com", profile.PortfolioUrl);
        Assert.Equal("Desde enero", profile.AvailabilityNote);
    }

    [Fact]
    public async Task SaveProfile_OnlyForFreelancers_AndWithAValidCategory()
    {
        var (db, service) = Setup();
        var company = await TestDb.AddUserAsync(db, Roles.HRAdmin, "Admin", "Demo S.R.L.");
        var freelancer = await TestDb.AddUserAsync(db, Roles.Freelancer);

        Assert.False((await service.SaveProfileAsync(company.UserId, "Desarrollo de software", 1, "x", null, 100m, true, null)).Succeeded);
        Assert.False((await service.SaveProfileAsync(freelancer.UserId, "Categoría inventada", 1, "x", null, 100m, true, null)).Succeeded);
        Assert.Empty(await db.FreelancerProfiles.ToListAsync());
    }

    // ---------- HU-06: búsqueda ----------

    [Fact]
    public async Task Search_ReturnsOnlyProfilesMeetingAllFilters_OrderedByRate()
    {
        var (db, service) = Setup();
        var u1 = await TestDb.AddUserAsync(db, Roles.Freelancer, "Uno");
        var u2 = await TestDb.AddUserAsync(db, Roles.Freelancer, "Dos");
        var u3 = await TestDb.AddUserAsync(db, Roles.Freelancer, "Tres");
        await TestDb.AddProfileAsync(db, u1, "Desarrollo de software", 1500m, true);
        await TestDb.AddProfileAsync(db, u2, "Desarrollo de software", 900m, true);
        await TestDb.AddProfileAsync(db, u3, "Desarrollo de software", 700m, false);

        var results = await service.SearchAsync("Desarrollo de software", 1500m, onlyAvailable: true);

        Assert.Equal(new[] { "Dos", "Uno" }, results.Select(p => p.User.FullName).ToList());
    }

    [Fact]
    public async Task Search_WithoutMatches_ReturnsAnEmptyList()
    {
        var (db, service) = Setup();
        var user = await TestDb.AddUserAsync(db, Roles.Freelancer);
        await TestDb.AddProfileAsync(db, user, "Desarrollo de software", 1500m, true);

        Assert.Empty(await service.SearchAsync("Marketing y ventas", null, false));
    }

    // ---------- HU-07: solicitudes de servicio ----------

    [Fact]
    public async Task RequestService_RegistersTheRequestWithTheCompanyDataPreloaded()
    {
        var (db, service) = Setup();
        var company = await TestDb.AddUserAsync(db, Roles.Recruiter, "Reclutador", "Demo S.R.L.");
        var profile = await TestDb.AddProfileAsync(db, await TestDb.AddUserAsync(db, Roles.Freelancer));

        var result = await service.RequestServiceAsync(profile.FreelancerProfileId, company.UserId);

        Assert.True(result.Succeeded);
        var request = await db.ServiceRequests.FirstAsync();
        Assert.Equal(ServiceRequestStatus.Sent, request.Status);
        Assert.Equal("Demo S.R.L.", request.CompanyName);
        Assert.Equal(company.UserId, request.RequestedByUserId);
    }

    [Fact]
    public async Task RequestService_OnlyCompanyUsers_OnlyAvailableProfessionals_AndNoDuplicates()
    {
        var (db, service) = Setup();
        var company = await TestDb.AddUserAsync(db, Roles.HRAdmin, "Admin", "Demo S.R.L.");
        var otherFreelancer = await TestDb.AddUserAsync(db, Roles.Freelancer, "Otro");
        var available = await TestDb.AddProfileAsync(db, await TestDb.AddUserAsync(db, Roles.Freelancer), available: true);
        var unavailable = await TestDb.AddProfileAsync(db, otherFreelancer, available: false);

        Assert.False((await service.RequestServiceAsync(available.FreelancerProfileId, otherFreelancer.UserId)).Succeeded);
        Assert.False((await service.RequestServiceAsync(unavailable.FreelancerProfileId, company.UserId)).Succeeded);
        Assert.False((await service.RequestServiceAsync(9999, company.UserId)).Succeeded);

        Assert.True((await service.RequestServiceAsync(available.FreelancerProfileId, company.UserId)).Succeeded);
        Assert.False((await service.RequestServiceAsync(available.FreelancerProfileId, company.UserId)).Succeeded);
        Assert.Equal(1, await db.ServiceRequests.CountAsync());
    }

    [Fact]
    public async Task Respond_TheProfessionalAcceptsOrRejects_AndOnlyOnce()
    {
        var (db, service) = Setup();
        var company = await TestDb.AddUserAsync(db, Roles.HRAdmin, "Admin", "Demo S.R.L.");
        var freelancer = await TestDb.AddUserAsync(db, Roles.Freelancer);
        var profile = await TestDb.AddProfileAsync(db, freelancer);
        var requestId = (await service.RequestServiceAsync(profile.FreelancerProfileId, company.UserId)).Value;

        var accepted = await service.RespondAsync(requestId, freelancer.UserId, accept: true);
        var again = await service.RespondAsync(requestId, freelancer.UserId, accept: false);

        Assert.True(accepted.Succeeded);
        Assert.False(again.Succeeded);
        var request = await db.ServiceRequests.FirstAsync();
        Assert.Equal(ServiceRequestStatus.Accepted, request.Status);
        Assert.NotNull(request.RespondedAt);
    }

    [Fact]
    public async Task Respond_AnotherFreelancerCannotAnswerSomeoneElsesRequest()
    {
        var (db, service) = Setup();
        var company = await TestDb.AddUserAsync(db, Roles.HRAdmin, "Admin", "Demo S.R.L.");
        var profile = await TestDb.AddProfileAsync(db, await TestDb.AddUserAsync(db, Roles.Freelancer));
        var intruder = await TestDb.AddUserAsync(db, Roles.Freelancer, "Intruso");
        var requestId = (await service.RequestServiceAsync(profile.FreelancerProfileId, company.UserId)).Value;

        var result = await service.RespondAsync(requestId, intruder.UserId, accept: true);

        Assert.False(result.Succeeded);
        Assert.Equal(ServiceRequestStatus.Sent, (await db.ServiceRequests.FirstAsync()).Status);
    }

    [Fact]
    public async Task Confirm_OnlyAfterAcceptance_AndOnlyByTheCompanyThatRequested()
    {
        var (db, service) = Setup();
        var company = await TestDb.AddUserAsync(db, Roles.HRAdmin, "Admin", "Demo S.R.L.");
        var otherCompany = await TestDb.AddUserAsync(db, Roles.Recruiter, "Otro", "Otra S.A.");
        var freelancer = await TestDb.AddUserAsync(db, Roles.Freelancer);
        var profile = await TestDb.AddProfileAsync(db, freelancer);
        var requestId = (await service.RequestServiceAsync(profile.FreelancerProfileId, company.UserId)).Value;

        Assert.False((await service.ConfirmAsync(requestId, company.UserId)).Succeeded);   // aún no aceptada

        await service.RespondAsync(requestId, freelancer.UserId, accept: true);

        Assert.False((await service.ConfirmAsync(requestId, otherCompany.UserId)).Succeeded); // otra empresa
        Assert.True((await service.ConfirmAsync(requestId, company.UserId)).Succeeded);

        var request = await db.ServiceRequests.FirstAsync();
        Assert.Equal(ServiceRequestStatus.Confirmed, request.Status);
        Assert.NotNull(request.ConfirmedAt);
    }

    [Fact]
    public async Task ARejectedRequest_AllowsSendingANewOne()
    {
        var (db, service) = Setup();
        var company = await TestDb.AddUserAsync(db, Roles.HRAdmin, "Admin", "Demo S.R.L.");
        var freelancer = await TestDb.AddUserAsync(db, Roles.Freelancer);
        var profile = await TestDb.AddProfileAsync(db, freelancer);
        var requestId = (await service.RequestServiceAsync(profile.FreelancerProfileId, company.UserId)).Value;
        await service.RespondAsync(requestId, freelancer.UserId, accept: false);

        var newRequest = await service.RequestServiceAsync(profile.FreelancerProfileId, company.UserId);

        Assert.True(newRequest.Succeeded);
        Assert.Equal(2, await db.ServiceRequests.CountAsync());
    }
}
