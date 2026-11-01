using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RelayHR.Web.Data;
using RelayHR.Web.Dtos;
using RelayHR.Web.Entities;
using RelayHR.Web.Services;
namespace RelayHR.Web.Tests.Services;
public sealed class ExpressVacancyPublicationServiceTests
{
    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    { public override DateTimeOffset GetUtcNow() => now; }
    private sealed class SaveBarrier : SaveChangesInterceptor
    {
        private int arrivals;
        private readonly TaskCompletionSource ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken ct = default)
        {
            if (Interlocked.Increment(ref arrivals) == 2) ready.TrySetResult();
            await ready.Task.WaitAsync(TimeSpan.FromSeconds(10),ct); return result;
        }
    }
    private sealed class Database : IAsyncDisposable
    {
        private readonly string path = Path.Combine(Path.GetTempPath(), "relayhr-hu05-"+Guid.NewGuid()+".db");
        public AppDbContext Open(SaveChangesInterceptor? interceptor = null)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite($"Data Source={path};Pooling=False");
            if (interceptor is not null) options.AddInterceptors(interceptor);
            return new(options.Options);
        }
        public async Task Seed()
        {
            await using var db=Open(); await db.Database.EnsureCreatedAsync();
            db.Users.AddRange(new User { Id=1,Name="RRHH 1" },new User { Id=2,Name="RRHH 2" });
            var exit=new EmployeeExit { Employee=new Employee { Name="Empleado" },EffectiveDate=new DateOnly(2026,11,1) };
            var vacancy=ExpressVacancy.CreateFromExit(exit,null,new DateTime(2026,10,31,12,0,0,DateTimeKind.Utc));
            vacancy.JobTitle="Analista"; vacancy.DepartmentId=1; vacancy.JobDescription="Operaciones";
            vacancy.Requirements="Experiencia"; vacancy.MissingFields=vacancy.CalculateMissingFields();
            db.ExpressVacancies.Add(vacancy); await db.SaveChangesAsync();
        }
        public ValueTask DisposeAsync() { File.Delete(path); return ValueTask.CompletedTask; }
    }
    [Theory]
    [InlineData(0,true)] [InlineData(47,true)] [InlineData(48,false)] [InlineData(72,false)]
    public async Task Measures_from_effective_exit_not_creation_and_does_not_block_late_publication(int hours,bool meetsTarget)
    {
        await using var database=new Database(); await database.Seed(); await using var db=database.Open();
        var clock=new FixedClock(new DateTimeOffset(2026,11,1,0,0,0,TimeSpan.Zero).AddHours(hours));
        var result=await new ExpressVacancyPublicationService(db,clock).PublishAsync(1,1,default);
        Assert.Equal(200,result.StatusCode); var dto=Assert.IsType<ExpressVacancyResponseDTO>(result.Body.Data);
        Assert.Equal((double)hours,dto.HoursToPublication); Assert.Equal(meetsTarget,dto.Meets48HourTarget);
        Assert.Equal(clock.GetUtcNow().UtcDateTime,dto.PublishedAt);
    }
    [Fact]
    public async Task Future_effective_exit_has_no_negative_duration_or_target_result()
    {
        await using var database=new Database(); await database.Seed(); await using var db=database.Open();
        var clock=new FixedClock(new DateTimeOffset(2026,10,31,12,0,0,TimeSpan.Zero));
        var result=await new ExpressVacancyPublicationService(db,clock).PublishAsync(1,1,default);
        Assert.Equal(200,result.StatusCode); var dto=Assert.IsType<ExpressVacancyResponseDTO>(result.Body.Data);
        Assert.True(dto.PublishedBeforeEffectiveExit); Assert.Null(dto.HoursToPublication); Assert.Null(dto.Meets48HourTarget);
    }
    [Fact]
    public async Task Concurrent_publications_have_exactly_one_winner_and_preserve_its_audit()
    {
        await using var database=new Database(); await database.Seed(); var barrier=new SaveBarrier();
        await using var first=database.Open(barrier); await using var second=database.Open(barrier);
        var clock1=new FixedClock(new DateTimeOffset(2026,11,2,1,0,0,TimeSpan.Zero));
        var clock2=new FixedClock(new DateTimeOffset(2026,11,2,2,0,0,TimeSpan.Zero));
        var results=await Task.WhenAll(new ExpressVacancyPublicationService(first,clock1).PublishAsync(1,1,default),
            new ExpressVacancyPublicationService(second,clock2).PublishAsync(1,2,default));
        Assert.Equal(new[]{200,409},results.Select(r=>r.StatusCode).OrderBy(s=>s).ToArray());
        var winner=Assert.IsType<ExpressVacancyResponseDTO>(results.Single(r=>r.StatusCode==200).Body.Data);
        await using var check=database.Open(); var stored=await check.ExpressVacancies.SingleAsync();
        Assert.Equal(winner.PublishedAt,stored.PublishedAt); Assert.Equal(winner.PublishedByUserId,stored.PublishedByUserId);
        Assert.Equal(ExpressVacancyStatus.Published,stored.Status); Assert.Equal(1,await check.ExpressVacancies.CountAsync());
    }
    [Fact]
    public async Task Concurrent_edit_and_publication_cannot_overwrite_each_other()
    {
        await using var database=new Database(); await database.Seed(); var barrier=new SaveBarrier();
        await using var first=database.Open(barrier); await using var second=database.Open(barrier);
        var clock=TimeProvider.System;
        var results=await Task.WhenAll(new ExpressVacancyPublicationService(first,clock).PublishAsync(1,1,default),
            new ExpressVacancyPublicationService(second,clock).UpdateAsync(1,2,new UpdateExpressVacancyDTO { JobTitle="Editado" },default));
        Assert.Equal(new[]{200,409},results.Select(r=>r.StatusCode).OrderBy(s=>s).ToArray());
        await using var check=database.Open(); var stored=await check.ExpressVacancies.SingleAsync();
        if(stored.Status==ExpressVacancyStatus.Published) { Assert.Equal("Analista",stored.JobTitle); Assert.Null(stored.UpdatedAt); }
        else { Assert.Equal("Editado",stored.JobTitle); Assert.Null(stored.PublishedAt); }
    }
    [Fact]
    public void SqlServer_model_matches_snapshot_and_migration_script_contains_audit_and_constraints()
    {
        using var db=new RelayHrDbContext(new DbContextOptionsBuilder<RelayHrDbContext>()
            .UseSqlServer("Server=localhost;Database=ScriptOnly;Integrated Security=True;TrustServerCertificate=True").Options);
        Assert.False(db.Database.HasPendingModelChanges());
        var script=db.GetService<IMigrator>().GenerateScript("20261101085114_ChecklistTrackingAndExitClosing");
        Assert.Contains("PublishedByUserId",script); Assert.Contains("Revision",script);
        Assert.Contains("CK_ExpressVacancies_Status",script); Assert.DoesNotContain("DROP TABLE",script);
    }

    [Theory]
    [InlineData(ExpressVacancyStatus.InReview)]
    [InlineData(ExpressVacancyStatus.Approved)]
    [InlineData(ExpressVacancyStatus.Discarded)]
    public async Task Other_states_cannot_be_edited_or_published(ExpressVacancyStatus state)
    {
        await using var database=new Database(); await database.Seed(); await using var db=database.Open();
        var vacancy=await db.ExpressVacancies.SingleAsync(); vacancy.Status=state; await db.SaveChangesAsync();
        var service=new ExpressVacancyPublicationService(db,TimeProvider.System);
        Assert.Equal(409,(await service.UpdateAsync(1,1,new UpdateExpressVacancyDTO { JobTitle="Cambio" },default)).StatusCode);
        Assert.Equal(409,(await service.PublishAsync(1,1,default)).StatusCode);
        Assert.Null(vacancy.PublishedAt); Assert.Null(vacancy.UpdatedAt);
    }
}
