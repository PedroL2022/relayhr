using Microsoft.Extensions.Options;

namespace RelayHR.Web.Services;

/// <summary>
/// Proceso en segundo plano que revisa periódicamente las solicitudes persistidas y genera
/// las vacantes. No depende de ninguna acción manual: al arrancar la aplicación retoma
/// de inmediato lo que haya quedado pendiente, incluso si se reinició a mitad de un proceso.
/// </summary>
public class ExpressVacancyWorker : BackgroundService
{
    private readonly IExpressVacancyRequestProcessor _processor;
    private readonly ExpressVacancyOptions _options;
    private readonly ILogger<ExpressVacancyWorker> _logger;

    public ExpressVacancyWorker(
        IExpressVacancyRequestProcessor processor,
        IOptions<ExpressVacancyOptions> options,
        ILogger<ExpressVacancyWorker> logger)
    {
        _processor = processor;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Generación automática de Vacantes Express iniciada (revisión cada {Interval} s, máximo {MaxAttempts} intentos).",
            _options.PollingIntervalSeconds, _options.MaxAttempts);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.PollingIntervalSeconds));

        try
        {
            // Primera pasada inmediata para retomar lo pendiente tras un reinicio.
            do
            {
                await RunCycleAsync(stoppingToken);
            }
            while (await timer.WaitNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // Apagado normal de la aplicación.
        }

        _logger.LogInformation("Generación automática de Vacantes Express detenida.");
    }

    private async Task RunCycleAsync(CancellationToken stoppingToken)
    {
        try
        {
            var completed = await _processor.ProcessPendingAsync(stoppingToken);

            if (completed > 0)
            {
                _logger.LogInformation("Ciclo terminado: {Completed} vacante(s) generada(s).", completed);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Un fallo (por ejemplo, base de datos no disponible) no debe detener el proceso:
            // se registra y se vuelve a intentar en el siguiente ciclo.
            _logger.LogError(ex, "Error en el ciclo de generación de Vacantes Express; se reintentará en el siguiente ciclo.");
        }
    }
}
