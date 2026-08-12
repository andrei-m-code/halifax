namespace Halifax.Domain.Exceptions;

/// <summary>
/// Base exception for domain errors raised by Halifax. The framework maps it to a failed
/// <see cref="Halifax.Domain.ApiResponse"/>; derived types indicate more specific conditions.
/// </summary>
public class HalifaxException : Exception
{
    /// <summary>
    /// Initializes a new instance with the specified error message.
    /// </summary>
    /// <param name="errorMessage">The message describing the error, exposed via <see cref="System.Exception.Message"/>.</param>
    public HalifaxException(string errorMessage) : base(errorMessage)
    {
    }
}
