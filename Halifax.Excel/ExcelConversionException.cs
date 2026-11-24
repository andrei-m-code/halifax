namespace Halifax.Excel;

/// <summary>
/// Thrown when an Excel cell cannot be converted to the type of the property it is read into.
/// </summary>
/// <remarks>
/// The message names the row and column as Excel shows them, so it can be passed on to whoever
/// uploaded the file. The original conversion error is the <see cref="Exception.InnerException"/>.
/// </remarks>
public class ExcelConversionException : FormatException
{
    /// <summary>
    /// Creates the exception for a cell that failed to convert.
    /// </summary>
    /// <param name="row">The 1-based row number, as Excel shows it.</param>
    /// <param name="column">The column header text.</param>
    /// <param name="value">The cell's text, as returned by <see cref="ExcelCell.GetText"/>.</param>
    /// <param name="targetType">The type the cell was read into.</param>
    /// <param name="innerException">The conversion error.</param>
    public ExcelConversionException(int row, string column, string? value, Type targetType, Exception innerException)
        : base($"Cannot read '{value}' in row {row}, column '{column}' as {Describe(targetType)}", innerException)
    {
        Row = row;
        Column = column;
        Value = value;
        TargetType = targetType;
    }

    /// <summary>The 1-based row number, as Excel shows it.</summary>
    public int Row { get; }

    /// <summary>The column header text.</summary>
    public string Column { get; }

    /// <summary>The cell's text.</summary>
    public string? Value { get; }

    /// <summary>The type the cell was read into.</summary>
    public Type TargetType { get; }

    private static string Describe(Type type) =>
        Nullable.GetUnderlyingType(type) is { } underlying ? $"{underlying.Name}?" : type.Name;
}
