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
            _ => (0, string.Empty, string.Empty)
        };

        if (statusCode == 0)
        {
            return false;
        }

        logger.LogWarning(exception, "La solicitud de salida fue rechazada: {Title}", title);
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
