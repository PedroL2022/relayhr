using RelayHR.Web.Entities;
using RelayHR.Web.Repository;

namespace RelayHR.Web.Services;

/// <summary>
/// Revisión, edición y publicación de las Vacantes Express (HU-05).
/// </summary>
public class VacancyService
{
    private readonly IVacancyRepository _vacancies;
    private readonly IDepartmentRepository _departments;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _time;

    public VacancyService(
        IVacancyRepository vacancies,
        IDepartmentRepository departments,
        IUnitOfWork unitOfWork,
        TimeProvider time)
    {
        _vacancies = vacancies;
        _departments = departments;
        _unitOfWork = unitOfWork;
        _time = time;
    }

    /// <summary>Guarda los cambios de la revisión. No se puede editar una vacante ya publicada.</summary>
    public async Task<Result> UpdateAsync(
        int vacancyId, string? jobTitle, int? departmentId, string? jobDescription, string? requirements,
        CancellationToken cancellationToken = default)
    {
        var vacancy = await _vacancies.GetByIdAsync(vacancyId, cancellationToken);

        if (vacancy is null)
        {
            return Result.Fail("La vacante no existe.");
        }

        if (!vacancy.CanEdit)
        {
            return Result.Fail("La vacante ya está publicada y no se puede editar.");
        }

        if (departmentId is not null &&
            !await _departments.AnyAsync(d => d.DepartmentId == departmentId, cancellationToken))
        {
            return Result.Fail("El departamento seleccionado no existe.");
        }

        vacancy.Edit(jobTitle, departmentId, jobDescription, requirements);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    /// <summary>
    /// Publica la vacante y registra la fecha y hora para medir la meta de 48 horas.
    /// Si faltan campos obligatorios no publica e indica cuáles son.
    /// </summary>
    public async Task<Result> PublishAsync(int vacancyId, string publishedBy, CancellationToken cancellationToken = default)
    {
        var vacancy = await _vacancies.GetWithExitForUpdateAsync(vacancyId, cancellationToken);

        if (vacancy is null)
        {
            return Result.Fail("La vacante no existe.");
        }

        if (vacancy.Status == VacancyStatus.Published)
        {
            return Result.Fail("La vacante ya estaba publicada.");
        }

        if (!vacancy.TryPublish(publishedBy, _time.GetUtcNow().UtcDateTime, out var missing))
        {
            return Result.Fail("No se puede publicar. Falta completar: " + string.Join(", ", missing) + ".");
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}
