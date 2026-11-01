using Microsoft.Extensions.Options;
using RelayHR.Web.Entities;
using RelayHR.Web.Repositories;

namespace RelayHR.Web.Services;

public sealed class ExitChecklistService(
    IEmployeeExitRepository repository,
    IOptions<ChecklistOptions> options) : IExitChecklistService
{
    // Cada salida debe incluir tareas para las tres áreas requeridas.
    private static readonly string[] RequiredDepartments =
        ["Tecnología", "Recursos Humanos", "Finanzas"];

    public async Task<ExitChecklist> GenerateAsync(
        EmployeeExit employeeExit,
        CancellationToken cancellationToken)
    {
        // Si el proceso se reintenta para una salida guardada, se devuelve el
        // checklist existente en lugar de duplicar sus tareas.
        if (employeeExit.Id > 0)
        {
            var existingChecklist = await repository.GetChecklistAsync(
                employeeExit.Id,
                cancellationToken);
            if (existingChecklist is not null)
            {
                return existingChecklist;
            }
        }

        var configuration = options.Value;
        ValidateTemplates(configuration);

        var templates = configuration.Tasks
            .Select(template => new
            {
                Template = template,
                ResponsibleUserKey = GetResponsibleUserKey(template.Department)
            })
            .ToArray();

        foreach (var assignment in templates)
        {
            if (!configuration.ResponsibleUserIds.TryGetValue(
                    assignment.ResponsibleUserKey,
                    out _))
            {
                throw new ChecklistConfigurationException(
                    $"Falta configurar Checklist:ResponsibleUserIds:{assignment.ResponsibleUserKey}.");
            }
        }

        // Valida departamentos y usuarios en bloque para limitar las consultas
        // independientemente de cuántas tareas contenga el catálogo.
        var departments = await repository.GetDepartmentsAsync(
            templates.Select(item => item.Template.Department)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            cancellationToken);
        var departmentsByName = departments.ToDictionary(
            department => department.Name,
            StringComparer.OrdinalIgnoreCase);
        foreach (var assignment in templates)
        {
            if (!departmentsByName.ContainsKey(assignment.Template.Department))
            {
                throw new ChecklistConfigurationException(
                    $"No existe el departamento '{assignment.Template.Department}'. Aplique las migraciones.");
            }
        }

        var userIds = templates
            .Select(item => configuration.ResponsibleUserIds[item.ResponsibleUserKey])
            .Distinct()
            .ToArray();
        var existingUserIds = await repository.GetExistingUserIdsAsync(
            userIds,
            cancellationToken);
        foreach (var assignment in templates)
        {
            var responsibleUserId =
                configuration.ResponsibleUserIds[assignment.ResponsibleUserKey];
            if (!existingUserIds.Contains(responsibleUserId))
            {
                throw new ChecklistConfigurationException(
                    $"El responsable configurado para '{assignment.Template.Department}' no existe.");
            }
        }

        var checklist = new ExitChecklist();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        foreach (var assignment in templates)
        {
            var template = assignment.Template;
            var dueDate = employeeExit.EffectiveDate
                .AddDays(-template.DaysBeforeEffectiveDate);
            if (dueDate < today)
            {
                dueDate = today;
            }

            checklist.Tasks.Add(new ChecklistTask
            {
                Department = departmentsByName[template.Department],
                ResponsibleUserId = configuration.ResponsibleUserIds[
                    assignment.ResponsibleUserKey],
                Title = template.Title,
                DueDate = dueDate,
                Status = ChecklistTaskStatus.Pending
            });
        }

        if (employeeExit.Id > 0)
        {
            return await repository.CreateChecklistIfMissingAsync(
                employeeExit.Id,
                checklist,
                cancellationToken);
        }

        return checklist;
    }

    private static void ValidateTemplates(ChecklistOptions configuration)
    {
        foreach (var department in RequiredDepartments)
        {
            if (!configuration.Tasks.Any(task =>
                    string.Equals(task.Department, department, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ChecklistConfigurationException(
                    $"Falta una plantilla de tarea para el departamento '{department}'.");
            }
        }

        if (configuration.Tasks.Any(task =>
                string.IsNullOrWhiteSpace(task.Department) ||
                string.IsNullOrWhiteSpace(task.Title) ||
                task.DaysBeforeEffectiveDate < 0))
        {
            throw new ChecklistConfigurationException(
                "Las plantillas requieren departamento, título y días antes de la fecha efectiva no negativos.");
        }
    }

    private static string GetResponsibleUserKey(string department) => department switch
    {
        "Tecnología" => "Technology",
        "Recursos Humanos" => "HumanResources",
        "Finanzas" => "Finance",
        _ => throw new ChecklistConfigurationException(
            $"No hay una clave de responsable definida para '{department}'.")
    };
}
