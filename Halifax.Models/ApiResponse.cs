using System;

namespace Halifax.Models
{
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

        public static ApiResponse Empty => new ApiResponse();
        
        public static ApiResponse<TData> With<TData>(TData data) => new ApiResponse<TData>(data);

        public static ApiResponse With(Exception exception) => new ApiResponse
        {
            Success = false,
            Error = new ApiResponseError(exception)
        };
    }
    
    public record ApiResponse<TData> : ApiResponse
    {
        public TData Data { get; set; }

        internal ApiResponse(TData data)
        {
            Data = data;
        }
    }
}
