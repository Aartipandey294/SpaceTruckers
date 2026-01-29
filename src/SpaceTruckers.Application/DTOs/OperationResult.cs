namespace SpaceTruckers.Application.DTOs;

/// <summary>
/// Represents the result of an operation with data.
/// </summary>
public sealed record OperationResult<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? ErrorMessage { get; init; }
    public string? ErrorCode { get; init; }

    public static OperationResult<T> Ok(T data) => new() { Success = true, Data = data };
    
    public static OperationResult<T> Fail(string message, string? code = null) => new()
    {
        Success = false,
        ErrorMessage = message,
        ErrorCode = code
    };
}

/// <summary>
/// Represents the result of an operation without data.
/// </summary>
public sealed record OperationResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public string? ErrorCode { get; init; }

    public static OperationResult Ok() => new() { Success = true };
    
    public static OperationResult Fail(string message, string? code = null) => new()
    {
        Success = false,
        ErrorMessage = message,
        ErrorCode = code
    };
}
