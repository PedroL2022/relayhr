namespace RelayHR.Web.Services
{
    /// <summary>La tarea solicitada no existe (404).</summary>
    public sealed class ChecklistTaskNotFoundException(int taskId)
        : Exception($"No existe la tarea del checklist con identificador {taskId}.");

    /// <summary>El estado enviado no es Pending, InProgress ni Completed (400).</summary>
    public sealed class InvalidTaskStatusException(string status)
        : Exception($"El estado '{status}' no es válido. Valores permitidos: Pending, InProgress, Completed.");

    /// <summary>El usuario no es de Recursos Humanos ni el responsable de la tarea (403).</summary>
    public sealed class TaskUpdateForbiddenException()
        : Exception("No tienes permiso para actualizar esta tarea.");

    /// <summary>El proceso de salida ya está cerrado y no admite cambios (409).</summary>
    public sealed class ClosedProcessException(string message) : Exception(message);
}
