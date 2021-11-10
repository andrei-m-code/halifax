namespace Halifax.Models;

public record ApiResponseError
{
    public string Type { get; }
    public string Message { get; }
    public string Trace { get; }

    public ApiResponseError(Exception exception)
    {
        Type = exception.GetType().Name;
        Message = exception.Message;
        Trace = exception.StackTrace;
    }
}

public record ApiResponse
{
    public ApiResponseError Error { get; private init; }
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
    public static ApiResponse Empty => new ApiResponse();

    /// <summary>
    /// Return API response with the data provided 
    /// </summary>
    public static ApiResponse<TData> With<TData>(TData data) => new ApiResponse<TData>(data);

    /// <summary>
    /// Return API response with exception info
    /// </summary>
    public static ApiResponse With(Exception exception) => new ApiResponse
    {
        Success = false,
        Error = new ApiResponseError(exception)
    };
}

public record ApiResponse<TData> : ApiResponse
{
    public TData Data { get; set; }

    public ApiResponse()
    {
    }

    public ApiResponse(TData data)
    {
        Data = data;
    }
}
