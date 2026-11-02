namespace RelayHR.Web.Services;

/// <summary>
/// Resultado de una operación de negocio: o salió bien, o trae un mensaje de error
/// listo para mostrar al usuario.
/// </summary>
public class Result
{
    public bool Succeeded { get; protected init; }
    public string? Error { get; protected init; }

    public static Result Ok() => new() { Succeeded = true };
    public static Result Fail(string error) => new() { Error = error };
}

public class Result<T> : Result
{
    public T? Value { get; private init; }

    public static Result<T> Ok(T value) => new() { Succeeded = true, Value = value };
    public new static Result<T> Fail(string error) => new() { Error = error };
}
