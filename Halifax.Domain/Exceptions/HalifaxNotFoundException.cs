namespace Halifax.Domain.Exceptions;

/// <summary>
/// Exception indicating that a requested resource does not exist. Typically translated to an
/// HTTP 404 response by the Halifax API layer.
/// </summary>
public class HalifaxNotFoundException : HalifaxException
{
    /// <summary>
    /// Initializes a new instance with the specified error message.
    /// </summary>
    /// <param name="errorMessage">The message describing the missing resource, exposed via <see cref="System.Exception.Message"/>.</param>
    public HalifaxNotFoundException(string errorMessage) : base(errorMessage)
    {
    }
}
