namespace Upkilo.API.Middleware;

/// <summary>
/// Standard API response envelope used consistently across all endpoints.
/// Wraps all responses with success flag, data payload, and optional error metadata.
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public string? ErrorCode { get; set; }
    public IDictionary<string, string[]>? Errors { get; set; }
    public ApiResponseMeta Meta { get; set; } = new();

    public static ApiResponse<T> Ok(T data, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message,
        };
    }

    public static ApiResponse<T> Fail(string message, string? errorCode = null, IDictionary<string, string[]>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode,
            Errors = errors,
        };
    }
}

public class ApiResponse : ApiResponse<object>
{
    public static ApiResponse Ok(string? message = null)
    {
        return new ApiResponse
        {
            Success = true,
            Message = message,
        };
    }

    public new static ApiResponse Fail(string message, string? errorCode = null, IDictionary<string, string[]>? errors = null)
    {
        return new ApiResponse
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode,
            Errors = errors,
        };
    }
}

/// <summary>
/// Pagination and timing metadata included in all API responses.
/// </summary>
public class ApiResponseMeta
{
    public string? RequestId { get; set; }
    public long? Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    public int? Page { get; set; }
    public int? PageSize { get; set; }
    public int? TotalCount { get; set; }
    public int? TotalPages { get; set; }
}
