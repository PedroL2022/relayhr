using RelayHR.Web.Entities;
using RelayHR.Web.Models;
using RelayHR.Web.Services;
using RelayHR.Web.Tests.Fakes;

namespace RelayHR.Web.Tests.Services;

public sealed class EmployeeExitServiceTests
{
    [Fact]
    public async Task CreateAsync_rejects_an_effective_date_in_the_past()
    {
        var repository = new FakeEmployeeExitRepository();
        var checklistService = new FakeExitChecklistService();
        var service = new EmployeeExitService(repository, checklistService);
        var request = new CreateEmployeeExitDTO
        {
            EmployeeId = 15,
            EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1)
        };

        await Assert.ThrowsAsync<InvalidEffectiveDateException>(
            () => service.CreateAsync(request, CancellationToken.None));

        Assert.Empty(repository.SavedExits);
        Assert.Equal(0, checklistService.GenerationCount);
    }

    [Fact]
    public async Task CreateAsync_rejects_an_employee_that_does_not_exist()
    {
        var repository = new FakeEmployeeExitRepository { EmployeeExists = false };
        var checklistService = new FakeExitChecklistService();
        var service = new EmployeeExitService(repository, checklistService);

        await Assert.ThrowsAsync<EmployeeNotFoundException>(() =>
            service.CreateAsync(
                ValidRequest(),
                CancellationToken.None));

        Assert.Empty(repository.SavedExits);
        Assert.Equal(0, checklistService.GenerationCount);
    }

    [Fact]
    public async Task CreateAsync_rejects_an_employee_with_an_open_exit()
    {
        var repository = new FakeEmployeeExitRepository { HasOpenExit = true };
        var service = new EmployeeExitService(repository, new FakeExitChecklistService());

        await Assert.ThrowsAsync<EmployeeExitConflictException>(() =>
            service.CreateAsync(
                ValidRequest(),
                CancellationToken.None));

        Assert.Empty(repository.SavedExits);
    }

    [Fact]
    public async Task CreateAsync_generates_and_persists_the_checklist_for_a_valid_request()
    {
        var repository = new FakeEmployeeExitRepository();
        var checklistService = new FakeExitChecklistService();
        var service = new EmployeeExitService(repository, checklistService);

        var response = await service.CreateAsync(
            ValidRequest(),
            CancellationToken.None);

        Assert.Equal(1, response.EmployeeExitId);
        Assert.Equal(101, response.ChecklistId);
        Assert.Single(repository.SavedExits);
        Assert.Single(repository.SavedExits[0].Checklist.Tasks);
        Assert.Equal(1, checklistService.GenerationCount);
    }

    private static CreateEmployeeExitDTO ValidRequest() => new()
    {
        EmployeeId = 15,
        EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(10)
    };
}
