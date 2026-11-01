using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RelayHR.Web.Services;

namespace RelayHR.Web.Middleware;

public sealed class ApiExceptionHandler(
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) = exception switch
        {
            EmployeeNotFoundException =>
                (StatusCodes.Status404NotFound, "Empleado no encontrado", exception.Message),
            InvalidEffectiveDateException =>
                (StatusCodes.Status400BadRequest, "Fecha efectiva inválida", exception.Message),
            EmployeeExitConflictException =>
                (StatusCodes.Status409Conflict, "Conflicto al registrar la salida", exception.Message),
            ChecklistConfigurationException =>
                (StatusCodes.Status503ServiceUnavailable, "Checklist no configurado", exception.Message),
            ChecklistTaskNotFoundException =>
                (StatusCodes.Status404NotFound, "Tarea no encontrada", exception.Message),
            InvalidTaskStatusException =>
                (StatusCodes.Status400BadRequest, "Estado inválido", exception.Message),
            TaskUpdateForbiddenException =>
                (StatusCodes.Status403Forbidden, "Acción no permitida", exception.Message),
            ClosedProcessException =>
    (StatusCodes.Status409Conflict, "Proceso cerrado", exception.Message),
            ExitNotFoundException =>
                (StatusCodes.Status404NotFound, "Salida no encontrada", exception.Message),
            _ => (0, string.Empty, string.Empty)
        };

        if (statusCode == 0)
        {
            return false;
        }

        logger.LogWarning(exception, "La solicitud fue rechazada: {Title}", title);
        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        }, cancellationToken);

        return true;
    }
}