using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RelayHR.Web.Data;
using RelayHR.Web.Entities;

namespace RelayHR.Web.Services;

public class ExpressVacancyService : IExpressVacancyService
{
    // Códigos de SQL Server para violación de índice único / restricción única.
    private const int UniqueIndexViolation = 2601;
    private const int UniqueConstraintViolation = 2627;

    private readonly AppDbContext _db;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ExpressVacancyService> _logger;

    public ExpressVacancyService(
        AppDbContext db,
        TimeProvider timeProvider,
        ILogger<ExpressVacancyService> logger)
    {
        _db = db;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<ExpressVacancy> GenerateForExitAsync(int employeeExitId, CancellationToken cancellationToken = default)
    {
        // 1. Idempotencia: si ya existe la vacante de esta salida, no se crea otra.
        var existing = await _db.ExpressVacancies
            .FirstOrDefaultAsync(v => v.EmployeeExitId == employeeExitId, cancellationToken);

        if (existing is not null)
        {
            _logger.LogInformation(
                "La salida {EmployeeExitId} ya tiene la vacante {ExpressVacancyId}; no se crea otra.",
                employeeExitId, existing.ExpressVacancyId);
            return existing;
        }

        // 2. Se recupera la salida con el empleado y el puesto que ocupaba.
        var employeeExit = await _db.EmployeeExits
            .Include(e => e.Employee)
                .ThenInclude(emp => emp.Position)
            .FirstOrDefaultAsync(e => e.Id == employeeExitId, cancellationToken)
            ?? throw new ExpressVacancyPermanentException(
                $"La salida {employeeExitId} no existe, no se puede generar la vacante.");

        // 3. Se genera la vacante con lo que haya disponible. Si el empleado no tiene puesto
        //    o al puesto le falta información, los campos quedan marcados como ToComplete.
        var position = employeeExit.Employee.Position;
        var vacancy = ExpressVacancy.CreateFromExit(
            employeeExit,
            position,
            _timeProvider.GetUtcNow().UtcDateTime);

        if (position is null)
        {
            _logger.LogWarning(
                "El empleado {EmployeeId} de la salida {EmployeeExitId} no tiene puesto asignado; la vacante se genera con todos los campos como {Marker}.",
                employeeExit.EmployeeId, employeeExitId, ExpressVacancy.ToComplete);
        }

        _db.ExpressVacancies.Add(vacancy);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // Otra ejecución creó la vacante al mismo tiempo. El índice único de la base de
            // datos lo impidió y aquí se devuelve la que ganó, sin tratarlo como error.
            _db.Entry(vacancy).State = EntityState.Detached;

            var winner = await _db.ExpressVacancies
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.EmployeeExitId == employeeExitId, cancellationToken);

            if (winner is null)
            {
                throw;
            }

            _logger.LogInformation(
                "La vacante de la salida {EmployeeExitId} fue creada en paralelo ({ExpressVacancyId}); se reutiliza.",
                employeeExitId, winner.ExpressVacancyId);
            return winner;
        }

        _logger.LogInformation(
            "Vacante {ExpressVacancyId} generada para la salida {EmployeeExitId}. Estado: {Status}. Campos pendientes: {MissingFields}.",
            vacancy.ExpressVacancyId, employeeExitId, vacancy.Status,
            vacancy.MissingFields.Count == 0 ? "ninguno" : string.Join(", ", vacancy.MissingFields));

        return vacancy;
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is SqlException { Number: UniqueIndexViolation or UniqueConstraintViolation };
}
