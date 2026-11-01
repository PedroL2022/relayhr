namespace RelayHR.Web.Services;

public sealed class EmployeeNotFoundException(int employeeId)
    : Exception($"No existe el empleado con identificador {employeeId}.");

public sealed class EmployeeExitConflictException(string message, Exception? innerException = null)
    : Exception(message, innerException);

public sealed class InvalidEffectiveDateException(string message) : Exception(message);

public sealed class ChecklistConfigurationException(string message) : Exception(message);
