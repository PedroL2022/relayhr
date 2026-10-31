namespace RelayHR.Web.Services.ServiceRequests;

public enum ServiceRequestResultStatus { Ok, Created, BadRequest, Forbidden, NotFound, Conflict }

public sealed class ServiceRequestResult<T>
{
    public ServiceRequestResultStatus Status { get; private init; }
    public string Message { get; private init; } = string.Empty;
    public T? Data { get; private init; }

    public bool IsSuccess => Status is ServiceRequestResultStatus.Ok or ServiceRequestResultStatus.Created;

    public static ServiceRequestResult<T> Ok(T data, string message) =>
        new() { Status = ServiceRequestResultStatus.Ok, Data = data, Message = message };

    public static ServiceRequestResult<T> Created(T data, string message) =>
        new() { Status = ServiceRequestResultStatus.Created, Data = data, Message = message };

    public static ServiceRequestResult<T> Fail(ServiceRequestResultStatus status, string message) =>
        new() { Status = status, Message = message };
}