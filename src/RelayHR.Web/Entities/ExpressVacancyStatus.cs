namespace RelayHR.Web.Entities;

/// <summary>
/// Estados posibles de una Vacante Express durante su ciclo de revisión.
/// Se guarda en la base de datos como texto para que sea legible al consultar la tabla.
/// </summary>
public enum ExpressVacancyStatus
{
    // Estado inicial: la vacante se generó automáticamente y nadie la ha revisado.
    // Los valores empiezan en 1 para que el valor por defecto de C# (0) nunca sea un estado válido.
    PendingReview = 1,

    // Un responsable de RRHH está revisando o completando los datos.
    InReview = 2,

    // La vacante fue revisada y aprobada.
    Approved = 3,

    // La vacante fue descartada.
    Discarded = 4,

    Published = 5
}
