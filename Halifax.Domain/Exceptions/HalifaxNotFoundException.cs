namespace Halifax.Domain.Exceptions;

/// <summary>
/// Halifax not found exception.
/// </summary>
public class HalifaxNotFoundException : HalifaxException
{
    /// <summary>
    /// Constructor that takes error message.
    /// </summary>
    public HalifaxNotFoundException(string errorMessage) : base(errorMessage)
    {
    }
}
