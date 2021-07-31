namespace Halifax.Core.Exceptions
{
    /// <summary>
    /// Halifax unauthorized exception.
    /// </summary>
    public class HalifaxUnauthorizedException : HalifaxException
    {
        /// <summary>
        /// Constructor that takes error message.
        /// </summary>
        public HalifaxUnauthorizedException(string errorMessage) : base(errorMessage)
        {
        }
    }
}
