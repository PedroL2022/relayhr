using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using RelayHR.Web.Data;
using RelayHR.Web.Dtos;
using RelayHR.Web.Entities;
namespace RelayHR.Web.Services;

public sealed record VacancyOperation(int StatusCode, ApiResponse<object> Body);

public sealed class ExpressVacancyPublicationService(AppDbContext db, TimeProvider clock)
{
    public async Task<VacancyOperation> UpdateAsync(int id, int userId, UpdateExpressVacancyDTO dto, CancellationToken ct)
    {
        var vacancy = await LoadAsync(id, ct);
        if (vacancy is null) return Fail(404, "Vacante Express no encontrada.");
        if (vacancy.Status != ExpressVacancyStatus.PendingReview) return Conflict();
        if (!await db.Users.AnyAsync(u => u.Id == userId, ct)) return Fail(401, "Identidad no válida.");
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(dto, new ValidationContext(dto), errors, true))
            return Fail(400, "Los datos no son válidos.", new { errors = errors.Select(e => e.ErrorMessage) });
        if (dto.HasDepartmentId && dto.DepartmentId.HasValue &&
            !await db.Departments.AnyAsync(d => d.Id == dto.DepartmentId.Value, ct))
            return Fail(400, "El departamento no existe.", new { errors = new { DepartmentId = "Seleccione un departamento existente." } });
        if (dto.HasJobTitle) vacancy.JobTitle = Clean(dto.JobTitle);
        if (dto.HasDepartmentId) vacancy.DepartmentId = dto.DepartmentId;
        if (dto.HasJobDescription) vacancy.JobDescription = Clean(dto.JobDescription);
        if (dto.HasRequirements) vacancy.Requirements = Clean(dto.Requirements);
        vacancy.MissingFields = vacancy.CalculateMissingFields();
        vacancy.UpdatedAt = clock.GetUtcNow().UtcDateTime;
        vacancy.UpdatedByUserId = userId;
        vacancy.Revision = Guid.NewGuid();
        return await SaveAsync(vacancy, "Cambios guardados correctamente.", ct);
    }

    public async Task<VacancyOperation> PublishAsync(int id, int userId, CancellationToken ct)
    {
        var vacancy = await LoadAsync(id, ct);
        if (vacancy is null) return Fail(404, "Vacante Express no encontrada.");
        if (vacancy.Status != ExpressVacancyStatus.PendingReview) return Conflict();
        if (!await db.Users.AnyAsync(u => u.Id == userId, ct)) return Fail(401, "Identidad no válida.");
        var missing = vacancy.CalculateMissingFields();
        if (vacancy.DepartmentId.HasValue && !await db.Departments.AnyAsync(d => d.Id == vacancy.DepartmentId, ct))
            missing.Add(ExpressVacancy.Fields.DepartmentId);
        if (missing.Count > 0)
            return Fail(400, "La vacante no puede publicarse porque faltan campos obligatorios.", new { missingFields = missing });
        if (vacancy.JobTitle!.Length > 150 || vacancy.JobDescription!.Length > 4000 || vacancy.Requirements!.Length > 4000)
            return Fail(400, "Los textos superan la longitud permitida: cargo 150, descripción y requisitos 4000.");
        vacancy.MissingFields = missing;
        vacancy.Status = ExpressVacancyStatus.Published;
        vacancy.PublishedAt = clock.GetUtcNow().UtcDateTime;
        vacancy.PublishedByUserId = userId;
        vacancy.Revision = Guid.NewGuid();
        return await SaveAsync(vacancy, "Vacante publicada correctamente.", ct);
    }
    private Task<ExpressVacancy?> LoadAsync(int id, CancellationToken ct) => db.ExpressVacancies
        .Include(v => v.Department).Include(v => v.EmployeeExit)
        .SingleOrDefaultAsync(v => v.ExpressVacancyId == id, ct);
    private async Task<VacancyOperation> SaveAsync(ExpressVacancy vacancy, string message, CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { db.ChangeTracker.Clear(); return Conflict(); }
        // Consulta sin tracking para reflejar el departamento actualizado.
        var saved = await db.ExpressVacancies.AsNoTracking().Include(v => v.Department).Include(v => v.EmployeeExit)
            .SingleAsync(v => v.ExpressVacancyId == vacancy.ExpressVacancyId, ct);
        return new(200, ApiResponse<object>.Ok(ExpressVacancyResponseDTO.FromEntity(saved), message));
    }
    private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    private static VacancyOperation Conflict() => Fail(409, "La vacante cambió o ya no está en revisión. Actualice la página.");
    private static VacancyOperation Fail(int status, string message, object? data = null) =>
        new(status, new ApiResponse<object> { Success = false, Message = message, Data = data });
}
