using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RelayHR.Web.Data;
using RelayHR.Web.Entities;

namespace RelayHR.Web.Services;

/// <summary>
/// Toma las solicitudes persistidas, genera cada vacante y gestiona reintentos y errores.
/// Cada solicitud se procesa en su propio scope (y su propio DbContext) para que un error
/// no deje estado sucio que afecte a las siguientes.
/// </summary>
public class ExpressVacancyRequestProcessor : IExpressVacancyRequestProcessor
{
    private const int MaxErrorLength = 2000;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ExpressVacancyOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ExpressVacancyRequestProcessor> _logger;

    public ExpressVacancyRequestProcessor(
        IServiceScopeFactory scopeFactory,
        IOptions<ExpressVacancyOptions> options,
        TimeProvider timeProvider,
        ILogger<ExpressVacancyRequestProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<int> ProcessPendingAsync(CancellationToken cancellationToken)
    {
        List<int> candidateIds;

        using (var scope = _scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var now = _timeProvider.GetUtcNow().UtcDateTime;

            await WarnAboutOverdueRequestsAsync(db, now, cancellationToken);

            // Pendientes cuya hora llegó, o en proceso con el bloqueo vencido
            // (la aplicación se cayó o se reinició a mitad del proceso).
            candidateIds = await db.ExpressVacancyRequests
                .AsNoTracking()
                .Where(r =>
                    (r.Status == ExpressVacancyRequestStatus.Pending && r.NextAttemptAt <= now) ||
                    (r.Status == ExpressVacancyRequestStatus.Processing && r.LockedUntil < now))
                .OrderBy(r => r.NextAttemptAt)
                .Select(r => r.ExpressVacancyRequestId)
                .Take(_options.BatchSize)
                .ToListAsync(cancellationToken);
        }

        var completed = 0;

        foreach (var requestId in candidateIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (await ProcessOneAsync(requestId, cancellationToken))
            {
                completed++;
            }
        }

        return completed;
    }

    private async Task<bool> ProcessOneAsync(int requestId, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var lockedUntil = now.AddSeconds(_options.LeaseSeconds);

        // Se reclama la solicitud con un UPDATE condicional. Si dos instancias de la aplicación
        // la intentan tomar a la vez, solo una afecta la fila y la otra la omite.
        var claimed = await db.ExpressVacancyRequests
            .Where(r => r.ExpressVacancyRequestId == requestId &&
                (
                    (r.Status == ExpressVacancyRequestStatus.Pending && r.NextAttemptAt <= now) ||
                    (r.Status == ExpressVacancyRequestStatus.Processing && r.LockedUntil < now)
                ))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(r => r.Status, ExpressVacancyRequestStatus.Processing)
                .SetProperty(r => r.LockedUntil, lockedUntil)
                .SetProperty(r => r.LastAttemptAt, now)
                .SetProperty(r => r.Attempts, r => r.Attempts + 1),
                cancellationToken);

        if (claimed == 0)
        {
            return false;
        }

        var request = await db.ExpressVacancyRequests
            .AsNoTracking()
            .FirstAsync(r => r.ExpressVacancyRequestId == requestId, cancellationToken);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Si la aplicación se cayó muchas veces con esta solicitud en proceso, no se reintenta sin fin.
            if (request.Attempts > _options.MaxAttempts)
            {
                throw new ExpressVacancyPermanentException(
                    $"La solicitud superó el máximo de {_options.MaxAttempts} intentos.");
            }

            var service = scope.ServiceProvider.GetRequiredService<IExpressVacancyService>();
            var vacancy = await service.GenerateForExitAsync(request.EmployeeExitId, cancellationToken);

            stopwatch.Stop();
            var processingMs = stopwatch.ElapsedMilliseconds;
            DateTime? completedAt = _timeProvider.GetUtcNow().UtcDateTime;

            await db.ExpressVacancyRequests
                .Where(r => r.ExpressVacancyRequestId == requestId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(r => r.Status, ExpressVacancyRequestStatus.Completed)
                    .SetProperty(r => r.CompletedAt, completedAt)
                    .SetProperty(r => r.ProcessingMilliseconds, (long?)processingMs)
                    .SetProperty(r => r.LockedUntil, (DateTime?)null)
                    .SetProperty(r => r.LastError, (string?)null),
                    CancellationToken.None);

            LogCompletion(request, vacancy.ExpressVacancyId, processingMs, completedAt.Value);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // La aplicación se está deteniendo. La solicitud queda en Processing y se retoma
            // cuando venza el bloqueo, sin perderse.
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            await RegisterFailureAsync(db, request, ex, stopwatch.ElapsedMilliseconds);
            return false;
        }
    }

    private async Task RegisterFailureAsync(
        AppDbContext db,
        ExpressVacancyRequest request,
        Exception exception,
        long elapsedMilliseconds)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var message = Truncate($"{exception.GetType().Name}: {exception.Message}");

        var isPermanent = exception is ExpressVacancyPermanentException;
        var attemptsExhausted = request.Attempts >= _options.MaxAttempts;

        if (isPermanent || attemptsExhausted)
        {
            await db.ExpressVacancyRequests
                .Where(r => r.ExpressVacancyRequestId == request.ExpressVacancyRequestId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(r => r.Status, ExpressVacancyRequestStatus.Failed)
                    .SetProperty(r => r.LockedUntil, (DateTime?)null)
                    .SetProperty(r => r.LastError, message),
                    CancellationToken.None);

            _logger.LogError(
                exception,
                "Solicitud {RequestId} (salida {EmployeeExitId}) marcada como Failed tras {Attempts} intento(s) ({Reason}). Requiere revisión manual. Duración del intento: {ElapsedMs} ms.",
                request.ExpressVacancyRequestId, request.EmployeeExitId, request.Attempts,
                isPermanent ? "error no recuperable" : "reintentos agotados", elapsedMilliseconds);
            return;
        }

        var nextAttemptAt = now + _options.GetRetryDelay(request.Attempts);

        await db.ExpressVacancyRequests
            .Where(r => r.ExpressVacancyRequestId == request.ExpressVacancyRequestId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(r => r.Status, ExpressVacancyRequestStatus.Pending)
                .SetProperty(r => r.NextAttemptAt, nextAttemptAt)
                .SetProperty(r => r.LockedUntil, (DateTime?)null)
                .SetProperty(r => r.LastError, message),
                CancellationToken.None);

        _logger.LogWarning(
            exception,
            "Error temporal en la solicitud {RequestId} (salida {EmployeeExitId}), intento {Attempts} de {MaxAttempts}. Se reintentará a las {NextAttemptAt:u}. Duración del intento: {ElapsedMs} ms.",
            request.ExpressVacancyRequestId, request.EmployeeExitId, request.Attempts,
            _options.MaxAttempts, nextAttemptAt, elapsedMilliseconds);
    }

    private void LogCompletion(ExpressVacancyRequest request, int vacancyId, long processingMs, DateTime completedAt)
    {
        // Tiempo total desde que se registró la salida hasta que la vacante está disponible.
        var sinceRegistration = completedAt - request.CreatedAt;
        var limit = TimeSpan.FromMinutes(_options.MaxAvailabilityMinutes);

        if (sinceRegistration <= limit)
        {
            _logger.LogInformation(
                "Vacante {ExpressVacancyId} disponible para la salida {EmployeeExitId}. Generación: {ProcessingMs} ms. Desde el registro de la salida: {ElapsedSeconds:F1} s (límite {LimitMinutes} min). Intentos: {Attempts}.",
                vacancyId, request.EmployeeExitId, processingMs, sinceRegistration.TotalSeconds,
                _options.MaxAvailabilityMinutes, request.Attempts);
        }
        else
        {
            _logger.LogWarning(
                "Vacante {ExpressVacancyId} de la salida {EmployeeExitId} disponible FUERA del límite: {ElapsedSeconds:F1} s desde el registro (límite {LimitMinutes} min). Generación: {ProcessingMs} ms. Intentos: {Attempts}.",
                vacancyId, request.EmployeeExitId, sinceRegistration.TotalSeconds,
                _options.MaxAvailabilityMinutes, processingMs, request.Attempts);
        }
    }

    // Avisa si hay solicitudes que llevan más del límite sin completarse.
    private async Task WarnAboutOverdueRequestsAsync(AppDbContext db, DateTime now, CancellationToken cancellationToken)
    {
        var threshold = now.AddMinutes(-_options.MaxAvailabilityMinutes);

        var overdue = await db.ExpressVacancyRequests
            .AsNoTracking()
            .Where(r => r.Status != ExpressVacancyRequestStatus.Completed && r.CreatedAt < threshold)
            .CountAsync(cancellationToken);

        if (overdue > 0)
        {
            _logger.LogWarning(
                "Hay {Overdue} solicitud(es) de Vacante Express sin completar después de {LimitMinutes} minutos desde el registro de la salida.",
                overdue, _options.MaxAvailabilityMinutes);
        }
    }

    private static string Truncate(string value) =>
        value.Length <= MaxErrorLength ? value : value[..MaxErrorLength];
}
