using RelayHR.Web.Entities;

namespace RelayHR.Web.Services;

public interface IExpressVacancyService
{
    /// <summary>
    /// Genera la Vacante Express de una salida. Es idempotente: si la vacante ya existe
    /// la devuelve en lugar de crear otra, así que se puede llamar varias veces sin riesgo.
    /// </summary>
    /// <exception cref="ExpressVacancyPermanentException">La salida no existe.</exception>
    Task<ExpressVacancy> GenerateForExitAsync(int employeeExitId, CancellationToken cancellationToken = default);
}
