using System.Text.Json.Serialization;

namespace AgentContextOS.DTOs;

public sealed class ApiResponse<T>
{
    public bool Success { get; init; }

    public int StatusCode { get; init; }

    public string Message { get; init; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public T? Data { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<ApiFieldError>? Errors { get; init; }

    public static ApiResponse<T> Ok(T data, string message = "Request successful", int statusCode = 200) =>
        new() { Success = true, StatusCode = statusCode, Message = message, Data = data };

    public static ApiResponse<T> Created(T data, string message = "Resource created") =>
        new() { Success = true, StatusCode = 201, Message = message, Data = data };

    public static ApiResponse<T> Fail(string message, int statusCode = 400, IEnumerable<ApiFieldError>? errors = null) =>
        new() { Success = false, StatusCode = statusCode, Message = message, Errors = errors };
}

public sealed record ApiFieldError(string Field, string Message);
