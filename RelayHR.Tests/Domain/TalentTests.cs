using RelayHR.Web.Entities;
using RelayHR.Web.Repository;

namespace RelayHR.Tests.Domain;

/// <summary>HU-06 (filtros del directorio) y HU-07 (estados de la solicitud).</summary>
public class TalentTests
{
    private static List<FreelancerProfile> Profiles() => new()
    {
        new() { FreelancerProfileId = 1, Category = "Desarrollo de software", HourlyRate = 1500m, IsAvailable = true },
        new() { FreelancerProfileId = 2, Category = "Desarrollo de software", HourlyRate = 800m, IsAvailable = false },
        new() { FreelancerProfileId = 3, Category = "Diseño gráfico y UX", HourlyRate = 900m, IsAvailable = true },
        new() { FreelancerProfileId = 4, Category = "Contabilidad y finanzas", HourlyRate = 1200m, IsAvailable = true }
    };

    private static List<int> Ids(string? category, decimal? maxRate, bool onlyAvailable) =>
        FreelancerRepository.ApplyFilters(Profiles().AsQueryable(), category, maxRate, onlyAvailable)
            .Select(p => p.FreelancerProfileId)
            .OrderBy(id => id)
            .ToList();

    [Fact]
    public void ApplyFilters_WithoutFilters_ReturnsEveryone() =>
        Assert.Equal(new[] { 1, 2, 3, 4 }, Ids(null, null, false));

    [Fact]
    public void ApplyFilters_ByCategory() =>
        Assert.Equal(new[] { 1, 2 }, Ids("Desarrollo de software", null, false));

    [Fact]
    public void ApplyFilters_ByMaximumRate() =>
        Assert.Equal(new[] { 2, 3, 4 }, Ids(null, 1200m, false));

    [Fact]
    public void ApplyFilters_OnlyAvailable() =>
        Assert.Equal(new[] { 1, 3, 4 }, Ids(null, null, true));

    [Fact]
    public void ApplyFilters_CombinesAllFilters_OnlyProfilesMeetingAllOfThemAreReturned() =>
        Assert.Equal(new[] { 1 }, Ids("Desarrollo de software", 1500m, true));

    [Fact]
    public void ApplyFilters_NoMatch_ReturnsEmptyList() =>
        Assert.Empty(Ids("Marketing y ventas", null, false));

    [Fact]
    public void ServiceRequest_AcceptedThenConfirmed()
    {
        var request = new ServiceRequest();
        var now = DateTime.UtcNow;

        Assert.True(request.Respond(accept: true, now));
        Assert.Equal(ServiceRequestStatus.Accepted, request.Status);
        Assert.Equal(now, request.RespondedAt);

        Assert.True(request.Confirm(now));
        Assert.Equal(ServiceRequestStatus.Confirmed, request.Status);
        Assert.Equal(now, request.ConfirmedAt);
    }

    [Fact]
    public void ServiceRequest_Rejected_CannotBeConfirmedOrAnsweredAgain()
    {
        var request = new ServiceRequest();

        Assert.True(request.Respond(accept: false, DateTime.UtcNow));

        Assert.Equal(ServiceRequestStatus.Rejected, request.Status);
        Assert.False(request.Confirm(DateTime.UtcNow));
        Assert.False(request.Respond(accept: true, DateTime.UtcNow));
        Assert.False(request.IsActive);
    }

    [Fact]
    public void ServiceRequest_CannotBeConfirmedBeforeTheProfessionalAccepts()
    {
        var request = new ServiceRequest();

        Assert.False(request.Confirm(DateTime.UtcNow));
        Assert.Equal(ServiceRequestStatus.Sent, request.Status);
        Assert.True(request.IsActive);
    }
}
