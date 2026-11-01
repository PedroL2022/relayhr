namespace RelayHR.Web.Services;

/// <summary>Plantillas y responsables predeterminados usados al crear cada checklist.</summary>
public sealed class ChecklistOptions
{
    public const string SectionName = "Checklist";

    /// <summary>Una entrada por tarea que debe generarse para cada salida.</summary>
    public List<ChecklistTaskTemplate> Tasks { get; set; } = [];

    /// <summary>
    /// UserId de cada área, con claves Technology, HumanResources y Finance.
    /// </summary>
    public Dictionary<string, int> ResponsibleUserIds { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>Describe el título y el plazo relativo de una tarea del checklist.</summary>
public sealed class ChecklistTaskTemplate
{
    /// <summary>Nombre del departamento correspondiente a la entidad Department.</summary>
    public string Department { get; set; } = string.Empty;

    /// <summary>Actividad que el responsable debe completar.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Días antes de la fecha efectiva; cero vence en la fecha efectiva.</summary>
    public int DaysBeforeEffectiveDate { get; set; }

    /// <summary>
    /// Obligatoria (true) u opcional (false). Si la plantilla no lo indica,
    /// la tarea se considera obligatoria para no dejar pasar un cierre por omisión.
    /// </summary>
    public bool IsRequired { get; set; } = true;
}