using System.Globalization;
using NPOI.SS.UserModel;

namespace Halifax.Excel;

/// <summary>
/// Reads Excel cell values from their raw content, without NPOI's <c>DataFormatter</c>.
/// </summary>
/// <remarks>
/// NPOI's <c>DataFormatter</c> (and <c>ICell.ToString()</c> on .xls cells) loads SkiaSharp at runtime,
/// which NPOI does not ship. Everything here works from the cell's stored number, text, boolean or
/// formula result, so reading Excel needs no SkiaSharp.
/// </remarks>
public static class ExcelCell
{
    // Highest Excel serial date (31/12/9999). Larger numbers are never treated as dates.
    private const double maxExcelDate = 2958465;

    /// <summary>
    /// Returns the text of a cell: a drop-in for <c>cell?.ToString()</c> that needs no SkiaSharp.
    /// </summary>
    /// <param name="cell">The cell to read, or <see langword="null"/> for a missing cell.</param>
    /// <returns>
    /// <see langword="null"/> for a missing cell; an empty string for a blank cell; the text of a text cell;
    /// <c>TRUE</c>/<c>FALSE</c> for a boolean; the error code (such as <c>#DIV/0!</c>) for an error.
    /// A date-formatted number becomes <c>yyyy-MM-dd</c>, or <c>yyyy-MM-dd HH:mm:ss</c> when it has a time.
    /// Any other number uses its cell format when the format is made only of <c>0 # , . %</c>
    /// (so <c>000000</c> keeps leading zeros and <c>#,##0.00</c> keeps separators), otherwise up to
    /// 15 significant digits in the invariant culture. A formula cell gives its last calculated result.
    /// </returns>
    public static string? GetText(ICell? cell)
    {
        if (cell == null)
        {
            return null;
        }

        return GetValueType(cell) switch
        {
            CellType.String => cell.StringCellValue,
            CellType.Boolean => cell.BooleanCellValue ? "TRUE" : "FALSE",
            CellType.Numeric => IsDate(cell)
                ? FormatDate(cell.DateCellValue!.Value)
                : FormatNumber(cell.NumericCellValue, cell.CellStyle?.GetDataFormatString()),
            CellType.Error => FormulaError.ForInt(cell.ErrorCellValue).String,
            _ => string.Empty
        };
    }

    /// <summary>
    /// Reads a cell as <paramref name="targetType"/>. Returns <see langword="false"/> when the cell holds
    /// no value: missing, blank, an error, or whitespace-only text for a non-text target.
    /// </summary>
    /// <exception cref="FormatException">The text cannot be parsed as the target type.</exception>
    /// <exception cref="InvalidCastException">The cell value cannot be converted to the target type.</exception>
    /// <exception cref="OverflowException">The number does not fit the target type.</exception>
    internal static bool TryGetValue(ICell? cell, Type targetType, CultureInfo culture, out object? value)
    {
        value = null;

        if (cell == null)
        {
            return false;
        }

        var target = Nullable.GetUnderlyingType(targetType) ?? targetType;

        switch (GetValueType(cell))
        {
            case CellType.String:
                var text = cell.StringCellValue;
                if (target == typeof(string) || target == typeof(object))
                {
                    value = text;
                    return true;
                }

                if (string.IsNullOrWhiteSpace(text))
                {
                    return false;
                }

                value = FromText(text.Trim(), target, culture);
                return true;

            case CellType.Numeric:
                value = target == typeof(string)
                    ? GetText(cell)
                    : FromNumber(cell, target);
                return true;

            case CellType.Boolean:
                var flag = cell.BooleanCellValue;
                value = target == typeof(string) ? (flag ? "TRUE" : "FALSE")
                    : target == typeof(bool) || target == typeof(object) ? flag
                    : Convert.ChangeType(flag ? 1 : 0, target, CultureInfo.InvariantCulture);
                return true;

            default:
                return false;
        }
    }

    /// <summary>True when the cell holds nothing worth reading: missing, blank, an error or whitespace text.</summary>
    internal static bool IsBlank(ICell? cell)
    {
        if (cell == null)
        {
            return true;
        }

        return GetValueType(cell) switch
        {
            CellType.String => string.IsNullOrWhiteSpace(cell.StringCellValue),
            CellType.Numeric or CellType.Boolean => false,
            _ => true
        };
    }

    private static CellType GetValueType(ICell cell) =>
        cell.CellType == CellType.Formula ? cell.CachedFormulaResultType : cell.CellType;

    private static bool IsDate(ICell cell) =>
        cell.NumericCellValue <= maxExcelDate && DateUtil.IsCellDateFormatted(cell);

    private static string FormatDate(DateTime value) =>
        value.TimeOfDay == TimeSpan.Zero
            ? value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            : value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

    private static string FormatNumber(double value, string? format)
    {
        // Excel and .NET agree on these placeholders, so the cell's own format can be applied directly.
        if (!string.IsNullOrEmpty(format) &&
            format.Any(c => c is '0' or '#') &&
            format.All(c => c is '0' or '#' or ',' or '.' or '%'))
        {
            return value.ToString(format, CultureInfo.InvariantCulture);
        }

        // Excel shows at most 15 significant digits, which also hides binary noise such as 0.30000000000000004.
        return value.ToString("G15", CultureInfo.InvariantCulture);
    }

    private static object FromNumber(ICell cell, Type target)
    {
        var number = cell.NumericCellValue;

        if (target == typeof(DateTime) || target == typeof(DateOnly) || target == typeof(TimeOnly) ||
            target == typeof(DateTimeOffset))
        {
            // DateCellValue honours the workbook's 1900/1904 date system whatever the cell format is.
            var date = cell.DateCellValue ?? throw new InvalidCastException($"{number} is not a valid Excel date");
            if (target == typeof(DateOnly)) return DateOnly.FromDateTime(date);
            if (target == typeof(TimeOnly)) return TimeOnly.FromDateTime(date);
            if (target == typeof(DateTimeOffset)) return new DateTimeOffset(date);
            return date;
        }

        if (target == typeof(TimeSpan)) return TimeSpan.FromDays(number);
        if (target == typeof(bool)) return number != 0;

        // A cell stores a double. Going through Excel's 15 significant digits gives 123.45m for 123.45,
        // where a direct conversion keeps the binary noise (123.45000000000000284217094304m).
        if (target == typeof(decimal))
        {
            return decimal.Parse(number.ToString("G15", CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        if (target == typeof(object)) return number;
        if (target.IsEnum) return Enum.ToObject(target, Convert.ToInt64(number, CultureInfo.InvariantCulture));

        return Convert.ChangeType(number, target, CultureInfo.InvariantCulture);
    }

    private static object FromText(string text, Type target, CultureInfo culture)
    {
        if (target.IsEnum) return Enum.Parse(target, text, ignoreCase: true);
        if (target == typeof(Guid)) return Guid.Parse(text);
        if (target == typeof(bool)) return ParseBool(text);
        if (target == typeof(DateTime)) return DateTime.Parse(text, culture);
        if (target == typeof(DateOnly)) return DateOnly.FromDateTime(DateTime.Parse(text, culture));
        if (target == typeof(TimeOnly)) return TimeOnly.Parse(text, culture);
        if (target == typeof(DateTimeOffset)) return DateTimeOffset.Parse(text, culture);
        if (target == typeof(TimeSpan)) return TimeSpan.Parse(text, culture);

        if (target == typeof(double) || target == typeof(float))
        {
            return Convert.ChangeType(double.Parse(text, NumberStyles.Any, culture), target, culture);
        }

        if (target.IsPrimitive || target == typeof(decimal))
        {
            return Convert.ChangeType(decimal.Parse(text, NumberStyles.Any, culture), target, culture);
        }

        return Convert.ChangeType(text, target, culture);
    }

    private static bool ParseBool(string text)
    {
        if (bool.TryParse(text, out var value)) return value;

        return text.ToLowerInvariant() switch
        {
            "1" or "yes" or "y" => true,
            "0" or "no" or "n" => false,
            _ => throw new FormatException($"'{text}' is not a boolean")
        };
    }
}
