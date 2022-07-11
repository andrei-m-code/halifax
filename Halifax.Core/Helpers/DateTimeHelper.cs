using Halifax.Domain.Exceptions;

namespace Halifax.Core.Helpers;

public static class DateTimeHelper
{
    /// <summary>
    /// Validate if timeframe is valid, e.g. From less than To date.
    /// </summary>
    public static void ValidateRange(DateTime? from, DateTime? to)
    {
        if (from.HasValue && to.HasValue && from > to)
        {
            throw new HalifaxException("From date has to be before the To date.");
        }
    }

    /// <summary>
    /// Check if datetime lies within the timeframe
    /// </summary>
    public static bool IsIn(DateTime? from, DateTime? to, DateTime pointInTime)
    {
        return 
            (!from.HasValue || from <= pointInTime) &&
            (!to.HasValue || to >= pointInTime);
    }
}