namespace RelayHR.Web.Services;

public interface IExpressVacancyRequestProcessor
{
    /// <summary>
    /// Procesa las solicitudes pendientes cuya hora ya llegó. Devuelve cuántas se completaron.
    /// </summary>
    Task<int> ProcessPendingAsync(CancellationToken cancellationToken);
}
