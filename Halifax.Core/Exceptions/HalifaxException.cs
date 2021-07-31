using System;

namespace Halifax.Core.Exceptions
{
    /// <summary>
    /// Generic Halifax Exception
    /// </summary>
    public class HalifaxException : Exception
    {
        /// <summary>
        /// Constructor that takes error message.
        /// </summary>
        public HalifaxException(string errorMessage) : base(errorMessage)
        {
        }
    }
}
