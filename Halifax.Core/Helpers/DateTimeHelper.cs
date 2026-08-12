using Halifax.Domain.Exceptions;

namespace Halifax.Core.Helpers;

/// <summary>
/// Helpers for validating date ranges, testing containment, and formatting dates.
/// </summary>
public static class DateTimeHelper
{
    /// <summary>
    /// Validates that a date range is well-ordered, i.e. <paramref name="from"/> is not after <paramref name="to"/>.
    /// </summary>
    /// <param name="from">The start of the range, or <see langword="null"/> for an open start.</param>
    /// <param name="to">The end of the range, or <see langword="null"/> for an open end.</param>
    /// <exception cref="HalifaxException">Thrown when both bounds are supplied and <paramref name="from"/> is later than <paramref name="to"/>.</exception>
    /// <remarks>A <see langword="null"/> bound is treated as open-ended, so ranges with a missing bound always pass validation.</remarks>
    public static void ValidateRange(DateTime? from, DateTime? to)
    {
        if (from.HasValue && to.HasValue && from > to)
        {
            throw new HalifaxException("From date has to be before the To date.");
        }
    }

    /// <summary>
    /// Determines whether a point in time falls within the inclusive range bounded by <paramref name="from"/> and <paramref name="to"/>.
    /// </summary>
    /// <param name="from">The inclusive start of the range, or <see langword="null"/> for an open start.</param>
    /// <param name="to">The inclusive end of the range, or <see langword="null"/> for an open end.</param>
    /// <param name="pointInTime">The point in time to test.</param>
    /// <returns><see langword="true"/> when <paramref name="pointInTime"/> lies within the range; otherwise <see langword="false"/>.</returns>
    /// <remarks>Both bounds are inclusive, and a <see langword="null"/> bound is treated as open-ended (that side is unconstrained).</remarks>
    public static bool IsIn(DateTime? from, DateTime? to, DateTime pointInTime)
    {
        return
            (!from.HasValue || from <= pointInTime) &&
            (!to.HasValue || to >= pointInTime);
    }

    /// <summary>
    /// Formats the date as an ISO 8601 UTC string (<c>yyyy-MM-ddTHH:mm:ssZ</c>).
    /// </summary>
    /// <param name="from">The date to format, or <see langword="null"/>.</param>
    /// <returns>The formatted string, or <see langword="null"/> when <paramref name="from"/> is <see langword="null"/>.</returns>
    /// <remarks>The value is formatted as-is; it is not converted to UTC, so the trailing <c>Z</c> is only accurate for values already expressed in UTC.</remarks>
    public static string? ToIsoFormat(this DateTime? from) => from?.ToString("yyyy-MM-ddTHH:mm:ssZ");
}
