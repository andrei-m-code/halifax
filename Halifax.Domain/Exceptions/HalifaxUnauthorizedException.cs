namespace Halifax.Domain.Exceptions;

/// <summary>
/// Exception indicating that the caller is not authenticated or lacks permission for the
/// requested operation. Typically translated to an HTTP 401 response by the Halifax API layer.
/// </summary>
public class HalifaxUnauthorizedException : HalifaxException
{
    /// <summary>
    /// Initializes a new instance with the specified error message.
    /// </summary>
    /// <param name="errorMessage">The message describing the authorization failure, exposed via <see cref="System.Exception.Message"/>.</param>
    public HalifaxUnauthorizedException(string errorMessage) : base(errorMessage)
    {
    }
}
