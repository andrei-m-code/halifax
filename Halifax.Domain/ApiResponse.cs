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
    /// Gets an empty successful response carrying no data.
    /// </summary>
    /// <value>A new successful <see cref="ApiResponse"/> with no <see cref="Error"/>.</value>
    public static ApiResponse Empty => new();

    /// <summary>
    /// Creates a successful response wrapping the supplied data.
    /// </summary>
    /// <typeparam name="TData">The type of the response data.</typeparam>
    /// <param name="data">The payload to return to the caller.</param>
    /// <returns>A successful <see cref="ApiResponse{TData}"/> whose <see cref="ApiResponse{TData}.Data"/> is <paramref name="data"/>.</returns>
    /// <example>
    /// <code>
    /// return ApiResponse.With(user);
    /// </code>
    /// </example>
    public static ApiResponse<TData> With<TData>(TData data) => new(data);

    /// <summary>
    /// Creates a failed response describing the supplied exception.
    /// </summary>
    /// <param name="exception">The exception to surface as <see cref="Error"/>.</param>
    /// <returns>An <see cref="ApiResponse"/> with <see cref="Success"/> set to <see langword="false"/> and <see cref="Error"/> populated from <paramref name="exception"/>.</returns>
    /// <example>
    /// <code>
    /// return ApiResponse.With(new HalifaxNotFoundException("User not found"));
    /// </code>
    /// </example>
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
