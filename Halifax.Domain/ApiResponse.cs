namespace Halifax.Domain;

/// <summary>
/// Represents error details extracted from an exception.
/// </summary>
public record ApiResponseError
{
    /// <summary>
    /// The exception type name.
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// The exception message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// The exception stack trace, if available.
    /// </summary>
    public string? Trace { get; }

    /// <summary>
    /// Creates an error from an exception.
    /// </summary>
    /// <param name="exception">The source exception.</param>
    public ApiResponseError(Exception exception)
    {
        Type = exception.GetType().Name;
        Message = exception.Message;
        Trace = exception.StackTrace;
    }
}

/// <summary>
/// Standard API response wrapper indicating success or failure.
/// </summary>
public record ApiResponse
{
    /// <summary>
    /// Error details if the response represents a failure.
    /// </summary>
    public ApiResponseError? Error { get; private init; }

    /// <summary>
    /// Whether the operation succeeded.
    /// </summary>
    public bool Success { get; private init; } = true;

    /// <summary>
    /// Default constructor. (it's often needed for deserialization)
    /// </summary>
    public ApiResponse()
    {
    }

    /// <summary>
    /// Empty API response
    /// </summary>
    public static ApiResponse Empty => new();

    /// <summary>
    /// Return API response with the data provided
    /// </summary>
    public static ApiResponse<TData> With<TData>(TData data) => new(data);

    /// <summary>
    /// Return API response with exception info
    /// </summary>
    public static ApiResponse With(Exception exception) => new()
    {
        Success = false,
        Error = new ApiResponseError(exception)
    };
}

/// <summary>
/// Standard API response wrapper with a typed data payload.
/// </summary>
/// <typeparam name="TData">The type of the response data.</typeparam>
public record ApiResponse<TData> : ApiResponse
{
    /// <summary>
    /// The response data payload.
    /// </summary>
    public TData? Data { get; set; }

    /// <summary>
    /// Default constructor for deserialization.
    /// </summary>
    public ApiResponse()
    {
    }

    /// <summary>
    /// Creates a successful response with the specified data.
    /// </summary>
    /// <param name="data">The response data.</param>
    public ApiResponse(TData data)
    {
        Data = data;
    }
}
