using RelayHR.Web.DTOs.KnowledgeTransfer;

namespace RelayHR.Web.Services.KnowledgeTransfer;

public class OperationResult<T>
{
    public int StatusCode { get; private init; }
    public bool Success { get; private init; }
    public string Message { get; private init; } = string.Empty;
    public T? Data { get; private init; }
    public IReadOnlyList<string> Errors { get; private init; } = Array.Empty<string>();

    public static OperationResult<T> Created(T data, string message) =>
        new() { StatusCode = 201, Success = true, Message = message, Data = data };

    public static OperationResult<T> NotFound(string message) =>
        new() { StatusCode = 404, Success = false, Message = message, Errors = new[] { message } };

    public static OperationResult<T> Invalid(IReadOnlyList<string> errors) =>
        new() { StatusCode = 400, Success = false, Message = "Los datos enviados no son válidos.", Errors = errors };

    public ApiResponse<T> ToResponse() => new()
    {
        Success = Success,
        Message = Message,
        Data = Data,
        Errors = Errors
    };
}
