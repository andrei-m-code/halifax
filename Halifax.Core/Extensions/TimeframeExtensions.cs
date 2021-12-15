using Halifax.Domain;
using Halifax.Domain.Exceptions;

namespace Halifax.Core.Exceptions;

public static class TimeframeExtensions
{
    /// <summary>
    /// Validate if timeframe is valid, e.g. From <= To date.
    /// </summary>
    /// <param name="timeframe"></param>
    public static void Validate(this Timeframe timeframe)
    {
        if (timeframe.From >= timeframe.To)
        {
            throw new HalifaxException("Timeframe From date has to be before the To date.");
        }
    }

    /// <summary>
    /// Check if datetime lies within the timeframe
    /// </summary>
    public static bool IsIn(this Timeframe timeframe, DateTime dateTime)
    {
        return 
            (!timeframe.From.HasValue || timeframe.From <= dateTime) &&
            (!timeframe.To.HasValue || timeframe.To >= dateTime);
    }
}