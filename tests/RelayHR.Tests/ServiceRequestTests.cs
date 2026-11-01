using System.Net;
using System.Net.Http.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RelayHR.Web.Data.ServiceRequests;
using RelayHR.Web.Entities;
using RelayHR.Web.Models.DTOs.ServiceRequests;
using RelayHR.Web.Services.ServiceRequests;

namespace RelayHR.Tests;

// =====================================================================================
// PRUEBAS UNITARIAS DEL SERVICIO (SQLite en memoria, sin HTTP)
// =====================================================================================
public sealed class ServiceRequestServiceTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly DbContextOptions<ServiceRequestDbContext> _options;
    private static int _seq = 100;

    public ServiceRequestServiceTests()
    {
        _connection.Open();
        _options = new DbContextOptionsBuilder<ServiceRequestDbContext>().UseSqlite(_connection).Options;
        using var db = new ServiceRequestDbContext(_options);
        db.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();

    private static int NextId() => Interlocked.Increment(ref _seq);

    // Único lugar a ajustar cuando FreelancerProfile real tenga más campos obligatorios.
    private int SeedProfile(int userId, bool active = true)
    {
        using var db = new ServiceRequestDbContext(_options);
        var profile = new FreelancerProfile { UserId = userId, IsActive = active };
        db.FreelancerProfiles.Add(profile);
        db.SaveChanges();
        return profile.Id;
    }

    private ServiceRequestService NewService() => new(new ServiceRequestDbContext(_options));

    private static CreateServiceRequestDTO Dto(int profileId) =>
        new() { FreelancerProfileId = profileId, Description = "Apoyo temporal." };

    private async Task<int> CreateAsync(int companyId, int profileId)
    {
        var r = await NewService().CreateAsync(companyId, Dto(profileId));
        Assert.Equal(ServiceRequestResultStatus.Created, r.Status);
        return r.Data!.ServiceRequestId;
    }

    [Fact]
    public async Task Create_ValidProfile_StartsAsSentWithUtcDate()
    {
        var company = NextId();
        var profile = SeedProfile(NextId());

        var result = await NewService().CreateAsync(company, Dto(profile));

        Assert.Equal(ServiceRequestResultStatus.Created, result.Status);
        Assert.Equal("Sent", result.Data!.Status);
        Assert.Equal(company, result.Data.CompanyId);
        Assert.InRange((DateTime.UtcNow - result.Data.CreatedAt).TotalSeconds, -5, 60);
    }

    [Fact]
    public async Task Create_ProfileDoesNotExist_ReturnsNotFound()
    {
        var result = await NewService().CreateAsync(NextId(), Dto(999_999));
        Assert.Equal(ServiceRequestResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Create_InactiveProfile_ReturnsBadRequest()
    {
        var profile = SeedProfile(NextId(), active: false);
        var result = await NewService().CreateAsync(NextId(), Dto(profile));
        Assert.Equal(ServiceRequestResultStatus.BadRequest, result.Status);
    }

    [Fact]
    public async Task Create_SecondActiveRequestSameCompanyAndProfile_ReturnsConflict()
    {
        var company = NextId();
        var profile = SeedProfile(NextId());
        await CreateAsync(company, profile);

        var second = await NewService().CreateAsync(company, Dto(profile));

        Assert.Equal(ServiceRequestResultStatus.Conflict, second.Status);
    }

    [Fact]
    public async Task FullFlow_SentAcceptedConfirmed_RecordsUtcDates()
    {
        var company = NextId(); var freelancer = NextId();
        var id = await CreateAsync(company, SeedProfile(freelancer));

        var responded = await NewService().RespondAsync(freelancer, id, "Accepted");
        Assert.Equal(ServiceRequestResultStatus.Ok, responded.Status);
        Assert.Equal("Accepted", responded.Data!.Status);
        Assert.InRange((DateTime.UtcNow - responded.Data.RespondedAt).TotalSeconds, -5, 60);

        var confirmed = await NewService().ConfirmAsync(company, id);
        Assert.Equal(ServiceRequestResultStatus.Ok, confirmed.Status);
        Assert.Equal("Confirmed", confirmed.Data!.Status);

        using var db = new ServiceRequestDbContext(_options);
        var row = await db.ServiceRequests.AsNoTracking().SingleAsync(r => r.ServiceRequestId == id);
        Assert.Equal(ServiceRequestStatus.Confirmed, row.Status);
        Assert.NotNull(row.RespondedAt);
        Assert.NotNull(row.ConfirmedAt);
        Assert.True(row.RespondedAt <= row.ConfirmedAt);
    }

    [Fact]
    public async Task Respond_Rejected_SetsRespondedAtAndNeverConfirmedAt()
    {
        var company = NextId(); var freelancer = NextId();
        var id = await CreateAsync(company, SeedProfile(freelancer));

        var result = await NewService().RespondAsync(freelancer, id, "Rejected");

        Assert.Equal(ServiceRequestResultStatus.Ok, result.Status);
        using var db = new ServiceRequestDbContext(_options);
        var row = await db.ServiceRequests.AsNoTracking().SingleAsync(r => r.ServiceRequestId == id);
        Assert.Equal(ServiceRequestStatus.Rejected, row.Status);
        Assert.NotNull(row.RespondedAt);
        Assert.Null(row.ConfirmedAt);
    }

    [Fact]
    public async Task Respond_Twice_SecondReturnsConflict()
    {
        var freelancer = NextId();
        var id = await CreateAsync(NextId(), SeedProfile(freelancer));

        await NewService().RespondAsync(freelancer, id, "Accepted");
        var second = await NewService().RespondAsync(freelancer, id, "Rejected");

        Assert.Equal(ServiceRequestResultStatus.Conflict, second.Status);
    }

    [Fact]
    public async Task Respond_ByAnotherFreelancer_ReturnsForbidden()
    {
        var id = await CreateAsync(NextId(), SeedProfile(NextId()));
        var result = await NewService().RespondAsync(NextId(), id, "Accepted");
        Assert.Equal(ServiceRequestResultStatus.Forbidden, result.Status);
    }

    [Theory]
    [InlineData("Confirmed")]
    [InlineData("Sent")]
    [InlineData("2")]
    [InlineData("")]
    public async Task Respond_InvalidValue_ReturnsBadRequest(string value)
    {
        var freelancer = NextId();
        var id = await CreateAsync(NextId(), SeedProfile(freelancer));
        var result = await NewService().RespondAsync(freelancer, id, value);
        Assert.Equal(ServiceRequestResultStatus.BadRequest, result.Status);
    }

    [Fact]
    public async Task Confirm_WhenSent_ReturnsConflict()
    {
        var company = NextId();
        var id = await CreateAsync(company, SeedProfile(NextId()));
        var result = await NewService().ConfirmAsync(company, id);
        Assert.Equal(ServiceRequestResultStatus.Conflict, result.Status);
    }

    [Fact]
    public async Task Confirm_WhenRejected_ReturnsConflict()
    {
        var company = NextId(); var freelancer = NextId();
        var id = await CreateAsync(company, SeedProfile(freelancer));
        await NewService().RespondAsync(freelancer, id, "Rejected");

        var result = await NewService().ConfirmAsync(company, id);

        Assert.Equal(ServiceRequestResultStatus.Conflict, result.Status);
    }

    [Fact]
    public async Task Confirm_ByAnotherCompany_ReturnsForbidden()
    {
        var freelancer = NextId();
        var id = await CreateAsync(NextId(), SeedProfile(freelancer));
        await NewService().RespondAsync(freelancer, id, "Accepted");

        var result = await NewService().ConfirmAsync(NextId(), id);

        Assert.Equal(ServiceRequestResultStatus.Forbidden, result.Status);
    }

    [Fact]
    public async Task GetById_OfAnotherCompany_ReturnsNotFound()
    {
        var id = await CreateAsync(NextId(), SeedProfile(NextId()));
        var result = await NewService().GetByIdAsync(NextId(), asFreelancer: false, id);
        Assert.Equal(ServiceRequestResultStatus.NotFound, result.Status);
    }
}

// =====================================================================================
// PRUEBAS DE INTEGRACIÓN HTTP (pipeline real + auth falsa + SQLite en memoria)
// =====================================================================================
public class ServiceRequestIntegrationTests : IClassFixture<RelayHrFactory>
{
    private static int _seq = 10_000;
    private readonly RelayHrFactory _factory;

    public ServiceRequestIntegrationTests(RelayHrFactory factory) => _factory = factory;

    private static int NextId() => Interlocked.Increment(ref _seq);

    private HttpClient ClientAs(int userId, string role)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Role", role);
        return client;
    }

    // Único lugar a ajustar cuando FreelancerProfile real tenga más campos obligatorios.
    private int SeedProfile(int freelancerUserId, bool active = true)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ServiceRequestDbContext>();
        var profile = new FreelancerProfile { UserId = freelancerUserId, IsActive = active };
        db.FreelancerProfiles.Add(profile);
        db.SaveChanges();
        return profile.Id;
    }

    private ServiceRequest LoadFromDb(int id)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ServiceRequestDbContext>();
        return db.ServiceRequests.AsNoTracking().Single(r => r.ServiceRequestId == id);
    }

    private static Task<HttpResponseMessage> PostCreate(HttpClient c, int profileId, string description = "Apoyo temporal en mantenimiento de sistemas.") =>
        c.PostAsJsonAsync("/api/service-requests", new { freelancerProfileId = profileId, description });

    private static Task<HttpResponseMessage> Respond(HttpClient c, int id, string response) =>
        c.SendAsync(new HttpRequestMessage(HttpMethod.Patch, $"/api/service-requests/{id}/respond")
        {
            Content = JsonContent.Create(new { response })
        });

    private static Task<HttpResponseMessage> Confirm(HttpClient c, int id) =>
        c.PostAsync($"/api/service-requests/{id}/confirm", null);

    private static async Task<T> ReadData<T>(HttpResponseMessage r)
    {
        var body = await r.Content.ReadFromJsonAsync<ServiceRequestApiResponse<T>>();
        return body!.Data!;
    }

    private static async Task<int> CreateOk(HttpClient company, int profileId)
    {
        var r = await PostCreate(company, profileId);
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        return (await ReadData<ServiceRequestCreatedDTO>(r)).ServiceRequestId;
    }

    private static void AssertRecentUtc(DateTime? value)
    {
        Assert.NotNull(value);
        Assert.InRange((DateTime.UtcNow - value!.Value).TotalSeconds, -5, 60);
    }

    // ------------------------------------------------------------------ creación

    [Fact]
    public async Task Create_Valid_Returns201_CompanyFromToken_StatusSent()
    {
        var companyId = NextId();
        var profileId = SeedProfile(NextId());

        // El body NO incluye companyId: sale de los claims.
        var response = await PostCreate(ClientAs(companyId, "Company"), profileId);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ServiceRequestApiResponse<ServiceRequestCreatedDTO>>();
        Assert.True(body!.Success);
        Assert.Equal("Solicitud de servicio registrada correctamente.", body.Message);
        Assert.Equal(companyId, body.Data!.CompanyId);
        Assert.Equal(profileId, body.Data.FreelancerProfileId);
        Assert.Equal("Sent", body.Data.Status);
        AssertRecentUtc(body.Data.CreatedAt);

        var row = LoadFromDb(body.Data.ServiceRequestId);
        Assert.Equal(companyId, row.CompanyId);
        Assert.Equal(ServiceRequestStatus.Sent, row.Status);
    }

    [Fact]
    public async Task Create_NonExistentProfile_Returns404()
    {
        var response = await PostCreate(ClientAs(NextId(), "Company"), 987_654);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_InactiveProfile_Returns400()
    {
        var profileId = SeedProfile(NextId(), active: false);
        var response = await PostCreate(ClientAs(NextId(), "Company"), profileId);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_EmptyDescription_Returns400WithEnvelope()
    {
        var profileId = SeedProfile(NextId());
        var response = await PostCreate(ClientAs(NextId(), "Company"), profileId, description: "");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ServiceRequestApiResponse<object>>();
        Assert.False(body!.Success);
    }

    [Fact]
    public async Task Create_RepeatedClick_SecondReturns409AndOnlyOneRowExists()
    {
        var companyId = NextId();
        var profileId = SeedProfile(NextId());
        var company = ClientAs(companyId, "Company");

        var first = await PostCreate(company, profileId);
        var second = await PostCreate(company, profileId);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ServiceRequestDbContext>();
        Assert.Equal(1, db.ServiceRequests.Count(r => r.CompanyId == companyId && r.FreelancerProfileId == profileId));
    }

    [Fact]
    public async Task Create_AfterPreviousWasRejected_IsAllowedAgain()
    {
        var freelancerId = NextId();
        var profileId = SeedProfile(freelancerId);
        var company = ClientAs(NextId(), "Company");

        var id = await CreateOk(company, profileId);
        await Respond(ClientAs(freelancerId, "Freelancer"), id, "Rejected");

        var again = await PostCreate(company, profileId);
        Assert.Equal(HttpStatusCode.Created, again.StatusCode);
    }

    [Fact]
    public async Task Create_AsFreelancer_Returns403()
    {
        var profileId = SeedProfile(NextId());
        var response = await PostCreate(ClientAs(NextId(), "Freelancer"), profileId);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_Anonymous_Returns401()
    {
        var response = await PostCreate(_factory.CreateClient(), 1);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ------------------------------------------------------------------ flujo de estados

    [Fact]
    public async Task Flow_Sent_Accepted_Confirmed_RecordsUtcDates()
    {
        var companyId = NextId(); var freelancerId = NextId();
        var profileId = SeedProfile(freelancerId);
        var company = ClientAs(companyId, "Company");
        var freelancer = ClientAs(freelancerId, "Freelancer");

        var id = await CreateOk(company, profileId);

        var respond = await Respond(freelancer, id, "Accepted");
        Assert.Equal(HttpStatusCode.OK, respond.StatusCode);
        var responded = await ReadData<ServiceRequestRespondedDTO>(respond);
        Assert.Equal("Accepted", responded.Status);
        AssertRecentUtc(responded.RespondedAt);

        var confirm = await Confirm(company, id);
        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);
        var body = await confirm.Content.ReadFromJsonAsync<ServiceRequestApiResponse<ServiceRequestConfirmedDTO>>();
        Assert.Equal("Servicio confirmado correctamente.", body!.Message);
        Assert.Equal("Confirmed", body.Data!.Status);
        AssertRecentUtc(body.Data.ConfirmedAt);

        var row = LoadFromDb(id);
        Assert.Equal(ServiceRequestStatus.Confirmed, row.Status);
        AssertRecentUtc(row.RespondedAt);
        AssertRecentUtc(row.ConfirmedAt);

        // Ya confirmada: no puede cambiar de estado nunca más.
        Assert.Equal(HttpStatusCode.Conflict, (await Confirm(company, id)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Respond(freelancer, id, "Rejected")).StatusCode);
    }

    [Fact]
    public async Task Flow_Sent_Rejected_SetsRespondedAtOnly()
    {
        var freelancerId = NextId();
        var company = ClientAs(NextId(), "Company");
        var id = await CreateOk(company, SeedProfile(freelancerId));

        var response = await Respond(ClientAs(freelancerId, "Freelancer"), id, "Rejected");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var row = LoadFromDb(id);
        Assert.Equal(ServiceRequestStatus.Rejected, row.Status);
        AssertRecentUtc(row.RespondedAt);
        Assert.Null(row.ConfirmedAt);
    }

    [Fact]
    public async Task Respond_Twice_Returns409()
    {
        var freelancerId = NextId();
        var id = await CreateOk(ClientAs(NextId(), "Company"), SeedProfile(freelancerId));
        var freelancer = ClientAs(freelancerId, "Freelancer");

        Assert.Equal(HttpStatusCode.OK, (await Respond(freelancer, id, "Accepted")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Respond(freelancer, id, "Accepted")).StatusCode);
    }

    [Fact]
    public async Task Respond_InvalidValue_Returns400()
    {
        var freelancerId = NextId();
        var id = await CreateOk(ClientAs(NextId(), "Company"), SeedProfile(freelancerId));

        var response = await Respond(ClientAs(freelancerId, "Freelancer"), id, "Confirmed");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Respond_NonExistentRequest_Returns404()
    {
        var response = await Respond(ClientAs(NextId(), "Freelancer"), 9_999_999, "Accepted");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Confirm_WhenSent_Returns409()
    {
        var company = ClientAs(NextId(), "Company");
        var id = await CreateOk(company, SeedProfile(NextId()));
        Assert.Equal(HttpStatusCode.Conflict, (await Confirm(company, id)).StatusCode);
    }

    [Fact]
    public async Task Confirm_WhenRejected_Returns409()
    {
        var freelancerId = NextId();
        var company = ClientAs(NextId(), "Company");
        var id = await CreateOk(company, SeedProfile(freelancerId));
        await Respond(ClientAs(freelancerId, "Freelancer"), id, "Rejected");

        Assert.Equal(HttpStatusCode.Conflict, (await Confirm(company, id)).StatusCode);
    }

    // ------------------------------------------------------------------ autorización

    [Fact]
    public async Task Respond_ByAnotherFreelancer_Returns403_AndStateUnchanged()
    {
        var id = await CreateOk(ClientAs(NextId(), "Company"), SeedProfile(NextId()));

        var response = await Respond(ClientAs(NextId(), "Freelancer"), id, "Accepted");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(ServiceRequestStatus.Sent, LoadFromDb(id).Status);
    }

    [Fact]
    public async Task Respond_AsCompany_Returns403()
    {
        var company = ClientAs(NextId(), "Company");
        var id = await CreateOk(company, SeedProfile(NextId()));
        Assert.Equal(HttpStatusCode.Forbidden, (await Respond(company, id, "Accepted")).StatusCode);
    }

    [Fact]
    public async Task Confirm_ByAnotherCompany_Returns403_AndStateUnchanged()
    {
        var freelancerId = NextId();
        var id = await CreateOk(ClientAs(NextId(), "Company"), SeedProfile(freelancerId));
        await Respond(ClientAs(freelancerId, "Freelancer"), id, "Accepted");

        var response = await Confirm(ClientAs(NextId(), "Company"), id);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(ServiceRequestStatus.Accepted, LoadFromDb(id).Status);
    }

    [Fact]
    public async Task Confirm_AsFreelancer_Returns403()
    {
        var freelancerId = NextId();
        var id = await CreateOk(ClientAs(NextId(), "Company"), SeedProfile(freelancerId));
        var freelancer = ClientAs(freelancerId, "Freelancer");
        await Respond(freelancer, id, "Accepted");

        Assert.Equal(HttpStatusCode.Forbidden, (await Confirm(freelancer, id)).StatusCode);
    }

    // ------------------------------------------------------------------ consultas

    [Fact]
    public async Task GetById_Owner_ReturnsCurrentStatus()
    {
        var company = ClientAs(NextId(), "Company");
        var id = await CreateOk(company, SeedProfile(NextId()));

        var response = await company.GetAsync($"/api/service-requests/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = await ReadData<ServiceRequestResponseDTO>(response);
        Assert.Equal(id, data.ServiceRequestId);
        Assert.Equal("Sent", data.Status);
    }

    [Fact]
    public async Task GetById_ChangingIdToAnotherCompanysRequest_Returns404()
    {
        var id = await CreateOk(ClientAs(NextId(), "Company"), SeedProfile(NextId()));

        var response = await ClientAs(NextId(), "Company").GetAsync($"/api/service-requests/{id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ByAnotherFreelancer_Returns404()
    {
        var id = await CreateOk(ClientAs(NextId(), "Company"), SeedProfile(NextId()));

        var response = await ClientAs(NextId(), "Freelancer").GetAsync($"/api/service-requests/{id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetList_ReturnsOnlyOwnRequests_ForCompanyAndFreelancer()
    {
        var companyAId = NextId(); var companyBId = NextId();
        var freelancer1 = NextId(); var freelancer2 = NextId();
        var profile1 = SeedProfile(freelancer1); var profile2 = SeedProfile(freelancer2);
        var companyA = ClientAs(companyAId, "Company");
        var companyB = ClientAs(companyBId, "Company");

        var a1 = await CreateOk(companyA, profile1);
        var a2 = await CreateOk(companyA, profile2);
        var b1 = await CreateOk(companyB, profile1);

        var listA = await ReadData<List<ServiceRequestResponseDTO>>(await companyA.GetAsync("/api/service-requests"));
        Assert.Equal(new[] { a1, a2 }.OrderBy(x => x), listA.Select(x => x.ServiceRequestId).OrderBy(x => x));
        Assert.All(listA, x => Assert.Equal(companyAId, x.CompanyId));

        var listF1 = await ReadData<List<ServiceRequestResponseDTO>>(
            await ClientAs(freelancer1, "Freelancer").GetAsync("/api/service-requests"));
        Assert.Equal(new[] { a1, b1 }.OrderBy(x => x), listF1.Select(x => x.ServiceRequestId).OrderBy(x => x));
    }

    [Fact]
    public async Task GetList_Anonymous_Returns401()
    {
        var response = await _factory.CreateClient().GetAsync("/api/service-requests");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}