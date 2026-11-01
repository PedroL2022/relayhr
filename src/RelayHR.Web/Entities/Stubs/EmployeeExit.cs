namespace RelayHR.Web.Entities.Stubs;

// TEMPORAL: stub de la entidad de la HU-01. Borrar cuando llegue el EmployeeExit real
// y ajustar los "using" que apuntan a RelayHR.Web.Entities.Stubs.
public class EmployeeExit
{
    public int Id { get; set; }

    /// <summary>Identificador del empleado que sale de la empresa.</summary>
    public int EmployeeId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
