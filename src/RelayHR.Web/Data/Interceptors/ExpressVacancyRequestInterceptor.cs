using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RelayHR.Web.Entities;

namespace RelayHR.Web.Data.Interceptors;

/// <summary>
/// Crea automáticamente una solicitud de Vacante Express cada vez que se guarda una
/// salida nueva (HU-01). La solicitud viaja en el mismo SaveChanges que la salida,
/// por lo que ambas se guardan juntas o ninguna se guarda. Así no hace falta modificar
/// el código que registra la salida y no existe el riesgo de guardar la salida sin su solicitud.
/// </summary>
public sealed class ExpressVacancyRequestInterceptor : SaveChangesInterceptor
{
    private readonly TimeProvider _timeProvider;

    public ExpressVacancyRequestInterceptor(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        EnqueueRequests(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        EnqueueRequests(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private void EnqueueRequests(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        // Solo las salidas nuevas generan solicitud; las modificaciones no.
        var newExits = context.ChangeTracker
            .Entries<EmployeeExit>()
            .Where(entry => entry.State == EntityState.Added)
            .Select(entry => entry.Entity)
            .ToList();

        if (newExits.Count == 0)
        {
            return;
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        foreach (var employeeExit in newExits)
        {
            // Se enlaza por navegación porque el Id de la salida aún no existe en este punto.
            context.Set<ExpressVacancyRequest>().Add(new ExpressVacancyRequest
            {
                EmployeeExit = employeeExit,
                Status = ExpressVacancyRequestStatus.Pending,
                NextAttemptAt = now,
                CreatedAt = now
            });
        }
    }
}
