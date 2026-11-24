using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using CsvHelper;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace Halifax.Excel;

/// <summary>
/// Converts between collections of strongly-typed objects and Excel (.xlsx/.xls) or CSV files.
/// Supports custom column-name mappings, excluded properties and automatic conversion between cell values
/// and property types.
/// </summary>
/// <typeparam name="TObject">The type of object to read from or write to Excel/CSV. When reading,
/// values are materialized onto this type; when writing, its public properties become columns.</typeparam>
/// <remarks>
/// A single converter instance can be reused across multiple read and write calls. Register any
/// custom header-to-property mappings with <see cref="AddMapping"/> and exclusions with <see cref="Exclude"/>
/// before calling a read or write method; unmapped properties fall back to the property name as the column header.
/// To put several sheets in one workbook, pass converters to <see cref="ExcelWorkbookWriter"/>.
/// Reading and writing never need SkiaSharp at runtime.
/// </remarks>
/// <example>
/// <code>
/// var converter = new ExcelConverter&lt;Person&gt;();
/// converter.AddMapping("Full Name", p =&gt; p.Name);
/// converter.Exclude(p =&gt; p.InternalId);
///
/// // Write to a stream as Excel; the stream stays open and is rewound
/// await converter.WriteExcelAsync(outputStream, people);
///
/// // Read back, auto-detecting the format from the content type
/// var loaded = await converter.ReadAsync(inputStream, "text/csv");
/// </code>
/// </example>
public class ExcelConverter<TObject>
{
    private const int defaultWidthToStringLengthFactor = 300;

    // Excel's limit: 255 characters of 256 width units each.
    private const int maxExcelColumnWidth = 255 * 256;

    /// <summary>
    /// Multiplier applied to the longest cell/header text length (in characters) when sizing each
    /// Excel column during <see cref="WriteExcelAsync"/>. Higher values produce wider columns. Default is 300.
    /// </summary>
    public int WidthToStringLengthFactor { get; set; } = defaultWidthToStringLengthFactor;

    /// <summary>
    /// Upper bound, in Excel width units, applied to every column written by <see cref="WriteExcelAsync"/>.
    /// A computed width larger than this is clamped down. Ignored when set to zero or less. Default is 9000 (30 * 300).
    /// Widths never exceed Excel's own limit of 255 characters.
    /// </summary>
    public int MaxCellWidth { get; set; } = 30 * defaultWidthToStringLengthFactor;

    /// <summary>
    /// Lower bound, in Excel width units, applied to every column written by <see cref="WriteExcelAsync"/>.
    /// A computed width smaller than this is clamped up. Ignored when set to zero or less. Default is 2700 (9 * 300).
    /// </summary>
    public int MinCellWidth { get; set; } = 9 * defaultWidthToStringLengthFactor;

    /// <summary>
    /// Excel number format applied to <see cref="DateTime"/> and <see cref="DateTimeOffset"/> cells written by
    /// <see cref="WriteExcelAsync"/>. The cell holds a real date; this only sets how Excel displays it.
    /// Default is <c>yyyy-mm-dd hh:mm:ss</c>.
    /// </summary>
    public string DateTimeFormat { get; set; } = "yyyy-mm-dd hh:mm:ss";

    /// <summary>
    /// Excel number format applied to <see cref="DateOnly"/> cells written by <see cref="WriteExcelAsync"/>.
    /// Default is <c>yyyy-mm-dd</c>.
    /// </summary>
    public string DateFormat { get; set; } = "yyyy-mm-dd";

    /// <summary>
    /// Culture used to parse and format values when reading and writing CSV (via <see cref="ReadCsvAsync"/>
    /// and <see cref="WriteCsvAsync"/>), and to parse Excel text cells into numbers and dates.
    /// Numeric and date Excel cells are read from their stored values and are not affected.
    /// Default is <see cref="CultureInfo.InvariantCulture"/>.
    /// </summary>
    public CultureInfo CultureInfo { get; set; } = CultureInfo.InvariantCulture;

    private readonly List<ColumnMapping<TObject>> mappings = [];
    private readonly HashSet<string> exclusions = new(StringComparer.Ordinal);

    /// <summary>
    /// Registers a mapping between a file column header and a property of <typeparamref name="TObject"/>.
    /// The mapping is applied by every subsequent read and write on this converter to translate
    /// between column headers and object properties.
    /// </summary>
    /// <param name="columnName">The column header text as it appears (or should appear) in the Excel or CSV file.
    /// Excel headers are matched case-insensitively and ignoring surrounding spaces.</param>
    /// <param name="propertyExpression">A lambda selecting the target property, e.g. <c>x =&gt; x.Name</c>.
    /// A boxing conversion in the body (such as a value-type property) is unwrapped automatically.</param>
    /// <remarks>
    /// Properties without an explicit mapping use their own name as the column header. Call this before
    /// invoking any read or write method; mappings added afterwards do not affect calls already made.
    /// </remarks>
    /// <exception cref="InvalidCastException">Thrown when <paramref name="propertyExpression"/> does not
    /// resolve to a member access (for example, a method call or a computed expression rather than a property).</exception>
    public void AddMapping(string columnName, Expression<Func<TObject, object>> propertyExpression)
    {
        var memberInfo = GetExpressionMemberInfo(propertyExpression);
        mappings.Add(new ColumnMapping<TObject>
        {
            ColumnName = columnName,
            PropertyName = memberInfo.Name,
            Expression = propertyExpression
        });
    }

    /// <summary>
    /// Leaves a property out of the file: it is not written as a column, and a column for it is ignored when reading.
    /// </summary>
    /// <param name="propertyExpression">A lambda selecting the property, e.g. <c>x =&gt; x.InternalId</c>.</param>
    /// <remarks>Exclusion wins over a mapping for the same property. Call this before reading or writing.</remarks>
    /// <exception cref="InvalidCastException">Thrown when <paramref name="propertyExpression"/> does not
    /// resolve to a member access.</exception>
    public void Exclude(Expression<Func<TObject, object>> propertyExpression)
    {
        exclusions.Add(GetExpressionMemberInfo(propertyExpression).Name);
    }

    /// <summary>
    /// Reads objects from a stream, choosing the Excel or CSV reader based on the supplied content type.
    /// </summary>
    /// <param name="stream">The stream to read from. The data must begin with a header row.</param>
    /// <param name="contentType">The MIME content type used to select a reader. Matching is case-insensitive:
    /// values containing <c>application/vnd.openxmlformats-officedocument</c> or <c>application/vnd.ms-excel</c>
    /// are read as Excel (see <see cref="ReadExcel(Stream)"/>; .xls and .xlsx are told apart from the content
    /// itself); values containing <c>csv</c> are read as CSV (see <see cref="ReadCsvAsync"/>).</param>
    /// <param name="cancellationToken">Token that cancels the operation; observed only for CSV reads.</param>
    /// <returns>The deserialized objects, in file order.</returns>
    /// <remarks>Registered <see cref="AddMapping"/> mappings are honored by whichever reader is selected.</remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> or <paramref name="contentType"/> is <see langword="null"/>.</exception>
    /// <exception cref="NotSupportedException">Thrown when <paramref name="contentType"/> matches neither the Excel nor the CSV patterns.</exception>
    public async Task<List<TObject>> ReadAsync(
        Stream stream,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        stream = stream ?? throw new ArgumentNullException(nameof(stream));
        contentType = (contentType ?? throw new ArgumentNullException(nameof(contentType))).ToLower();

        if (contentType.Contains("application/vnd.openxmlformats-officedocument") ||
            contentType.Contains("application/vnd.ms-excel"))
        {
            return ReadExcel(stream);
        }

        if (contentType.Contains("csv"))
        {
            return await ReadCsvAsync(stream, cancellationToken);
        }

        throw new NotSupportedException($"Content type {contentType} is not supported");
    }

    #region Excel

    /// <summary>
    /// Reads objects from the first worksheet of an Excel (.xlsx or .xls) stream. The first row is the header row.
    /// </summary>
    /// <param name="stream">The Excel stream to read from.</param>
    /// <returns>The deserialized objects, one per non-blank data row.</returns>
    /// <remarks>
    /// <para>Columns are matched to properties (or constructor parameters) by header text, case-insensitively;
    /// registered <see cref="AddMapping"/> mappings translate headers to property names first. Columns that match
    /// nothing, and excluded properties, are ignored. Rows whose matched cells are all blank are skipped. A blank
    /// cell leaves its property at its default (constructor parameters receive <see langword="null"/> or the
    /// type's default).</para>
    /// <para>Cells are read from their stored values, so no SkiaSharp is needed: numbers convert to numeric,
    /// <see cref="bool"/>, enum and date properties; dates fill <see cref="DateTime"/>, <see cref="DateOnly"/>,
    /// <see cref="TimeOnly"/> and <see cref="DateTimeOffset"/>; text is parsed with <see cref="CultureInfo"/>; a
    /// formula gives its last calculated result. A number read into a <see cref="string"/> property follows
    /// <see cref="ExcelCell.GetText"/>, so <c>000000</c>-formatted cells keep their leading zeros.</para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <see langword="null"/>.</exception>
    /// <exception cref="ExcelConversionException">A cell cannot be converted to its property's type; the
    /// exception names the row and column.</exception>
    public List<TObject> ReadExcel(Stream stream) => ReadSheet(stream, null);

    /// <summary>
    /// Reads objects from the named worksheet of an Excel (.xlsx or .xls) stream. The first row is the header row.
    /// </summary>
    /// <param name="stream">The Excel stream to read from.</param>
    /// <param name="sheetName">The worksheet to read, matched as Excel does (case-insensitively).</param>
    /// <returns>The deserialized objects, one per non-blank data row.</returns>
    /// <remarks>Reads exactly like <see cref="ReadExcel(Stream)"/>, from the given sheet.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The sheet name is empty or the workbook has no such sheet.</exception>
    /// <exception cref="ExcelConversionException">A cell cannot be converted to its property's type.</exception>
    public List<TObject> ReadExcel(Stream stream, string sheetName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sheetName);
        return ReadSheet(stream, sheetName);
    }

    /// <summary>
    /// Writes objects to an Excel (.xlsx) stream as a single worksheet with a styled header row and sized columns.
    /// </summary>
    /// <param name="stream">The stream that receives the workbook. It is left open and, when seekable,
    /// rewound to where the workbook starts, so it can be returned or uploaded straight away.</param>
    /// <param name="records">The objects to write, one per row, in enumeration order. Enumerated once.</param>
    /// <param name="sheetName">The worksheet name. Default is "Sheet 0".</param>
    /// <returns>A completed task; the work is performed synchronously.</returns>
    /// <remarks>
    /// One column is emitted per public instance property of <typeparamref name="TObject"/>, in reflection order,
    /// except those passed to <see cref="Exclude"/>. Header cells use the mapped column name from
    /// <see cref="AddMapping"/> when one exists, otherwise the property name, and are rendered bold on a
    /// light-yellow fill. Column widths are derived from the widest value using
    /// <see cref="WidthToStringLengthFactor"/> and clamped to <see cref="MinCellWidth"/>/<see cref="MaxCellWidth"/>.
    /// Numbers (every integer type, <see cref="float"/>, <see cref="double"/>, <see cref="decimal"/>) and
    /// <see cref="bool"/> are written as native Excel values; <see cref="DateTime"/>, <see cref="DateTimeOffset"/>
    /// (as its <see cref="DateTimeOffset.DateTime"/>) and <see cref="DateOnly"/> as dates shown with
    /// <see cref="DateTimeFormat"/>/<see cref="DateFormat"/>; any other type as its string representation.
    /// <see langword="null"/> values leave the cell empty. To write several sheets into one workbook, use
    /// <see cref="ExcelWorkbookWriter"/>.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> or <paramref name="records"/> is <see langword="null"/>.</exception>
    public Task WriteExcelAsync(Stream stream, IEnumerable<TObject> records, string sheetName = "Sheet 0")
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(records);

        using var workbook = new XSSFWorkbook();
        WriteSheet(workbook, new ExcelStyles(workbook), sheetName, records);
        ExcelStyles.Write(workbook, stream);

        return Task.CompletedTask;
    }

    internal void WriteSheet(IWorkbook workbook, ExcelStyles styles, string sheetName, IEnumerable<TObject> records)
    {
        var sheet = workbook.CreateSheet(sheetName);
        var properties = GetWrittenProperties();
        var rows = records
            .Select(record => properties.Select(p => p.GetValue(record)).ToArray())
            .ToList();

        var headerRow = sheet.CreateRow(0);
        for (var colIndex = 0; colIndex < properties.Length; colIndex++)
        {
            var property = properties[colIndex];
            var header = GetColumnName(property);
            var cell = headerRow.CreateCell(colIndex);
            cell.SetCellValue(header);
            cell.CellStyle = styles.Header;

            var maxLength = rows.Count > 0 ? rows.Max(values => GetDisplayLength(values[colIndex])) : 0;
            var width = Math.Max(maxLength, header.Length) * WidthToStringLengthFactor;
            if (MinCellWidth > 0) width = Math.Max(width, MinCellWidth);
            if (MaxCellWidth > 0) width = Math.Min(width, MaxCellWidth);
            sheet.SetColumnWidth(colIndex, Math.Clamp(width, 0, maxExcelColumnWidth));
        }

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var row = sheet.CreateRow(rowIndex + 1);
            var values = rows[rowIndex];
            for (var colIndex = 0; colIndex < properties.Length; colIndex++)
            {
                SetCellValue(row.CreateCell(colIndex), values[colIndex], styles);
            }
        }
    }

    private List<TObject> ReadSheet(Stream stream, string? sheetName)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var workbook = WorkbookFactory.Create(stream);

        ISheet? sheet;
        if (sheetName == null)
        {
            sheet = workbook.NumberOfSheets > 0 ? workbook.GetSheetAt(0) : null;
        }
        else
        {
            sheet = workbook.GetSheet(sheetName)
                    ?? throw new ArgumentException($"The workbook has no sheet named '{sheetName}'", nameof(sheetName));
        }

        var headerRow = sheet?.GetRow(sheet.FirstRowNum);
        if (sheet == null || headerRow == null)
        {
            return [];
        }

        var members = GetReadMembers();
        var constructorParameters = GetConstructorParameterNames();
        var columns = new List<ReadColumn>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var colIndex = Math.Max(0, (int)headerRow.FirstCellNum); colIndex < headerRow.LastCellNum; colIndex++)
        {
            var header = ExcelCell.GetText(headerRow.GetCell(colIndex))?.Trim();
            if (string.IsNullOrEmpty(header))
            {
                continue;
            }

            var memberName = mappings
                .FirstOrDefault(m => string.Equals(m.ColumnName.Trim(), header, StringComparison.OrdinalIgnoreCase))
                ?.PropertyName ?? header;

            if (!members.TryGetValue(memberName, out var member) || exclusions.Contains(member.Name) || !seen.Add(member.Name))
            {
                continue;
            }

            columns.Add(new ReadColumn(colIndex, header, member.Name, member.Type));
        }

        var records = new List<TObject>();

        for (var rowIndex = headerRow.RowNum + 1; rowIndex <= sheet.LastRowNum; rowIndex++)
        {
            var row = sheet.GetRow(rowIndex);
            if (row == null || columns.All(c => ExcelCell.IsBlank(row.GetCell(c.Index))))
            {
                continue;
            }

            var data = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            foreach (var column in columns)
            {
                var cell = row.GetCell(column.Index);
                object? value;

                try
                {
                    if (!ExcelCell.TryGetValue(cell, column.Type, CultureInfo, out value))
                    {
                        // A blank cell leaves a property at its default, but a constructor still needs an argument.
                        if (constructorParameters.Contains(column.Member))
                        {
                            data[column.Member] = null;
                        }

                        continue;
                    }
                }
                catch (Exception e) when (e is FormatException or InvalidCastException or OverflowException or ArgumentException)
                {
                    throw new ExcelConversionException(rowIndex + 1, column.Header, ExcelCell.GetText(cell), column.Type, e);
                }

                data[column.Member] = value;
            }

            records.Add(ObjectActivator.Activate<TObject>(data));
        }

        return records;
    }

    private void SetCellValue(ICell cell, object? value, ExcelStyles styles)
    {
        switch (value)
        {
            case null:
                return;

            case string valueString:
                cell.SetCellValue(valueString);
                break;

            case bool valueBool:
                cell.SetCellValue(valueBool);
                break;

            case DateTime valueDateTime:
                cell.SetCellValue(valueDateTime);
                cell.CellStyle = styles.ForFormat(DateTimeFormat);
                break;

            case DateTimeOffset valueDateTimeOffset:
                cell.SetCellValue(valueDateTimeOffset.DateTime);
                cell.CellStyle = styles.ForFormat(DateTimeFormat);
                break;

            case DateOnly valueDateOnly:
                cell.SetCellValue(valueDateOnly.ToDateTime(TimeOnly.MinValue));
                cell.CellStyle = styles.ForFormat(DateFormat);
                break;

            case byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal:
                cell.SetCellValue(Convert.ToDouble(value, CultureInfo.InvariantCulture));
                break;

            default:
                cell.SetCellValue(Convert.ToString(value));
                break;
        }
    }

    private int GetDisplayLength(object? value) => value switch
    {
        null => 0,
        DateTime or DateTimeOffset => DateTimeFormat.Length,
        DateOnly => DateFormat.Length,
        _ => Convert.ToString(value)?.Length ?? 0
    };

    // Properties that can be read from a file: settable properties and constructor parameters.
    private static Dictionary<string, (string Name, Type Type)> GetReadMembers()
    {
        var members = new Dictionary<string, (string Name, Type Type)>(StringComparer.OrdinalIgnoreCase);

        foreach (var property in typeof(TObject).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.CanWrite && property.GetIndexParameters().Length == 0)
            {
                members.TryAdd(property.Name, (property.Name, property.PropertyType));
            }
        }

        foreach (var parameter in typeof(TObject).GetConstructors().SelectMany(c => c.GetParameters()))
        {
            if (parameter.Name != null)
            {
                members.TryAdd(parameter.Name, (parameter.Name, parameter.ParameterType));
            }
        }

        return members;
    }

    private static HashSet<string> GetConstructorParameterNames() =>
        typeof(TObject).GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Where(p => p.Name != null)
            .Select(p => p.Name!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private sealed record ReadColumn(int Index, string Header, string Member, Type Type);

    #endregion

    #region CSV

    /// <summary>
    /// Reads objects from a CSV stream. The first line is treated as the header row and is used to map
    /// each subsequent line onto a new <typeparamref name="TObject"/>.
    /// </summary>
    /// <param name="stream">The CSV stream to read from, parsed with <see cref="CultureInfo"/>. It is left open.</param>
    /// <param name="cancellationToken">Token that cancels the read.</param>
    /// <returns>The deserialized objects, one per data line.</returns>
    /// <remarks>
    /// Header text is translated to a property name via registered <see cref="AddMapping"/> mappings, falling back
    /// to the header text itself; columns for excluded properties are ignored. Each row is materialized with
    /// <see cref="ObjectActivator"/>, which selects the constructor best matching the available columns and then
    /// sets any remaining writable public properties; individual values that cannot be converted to the target
    /// type are skipped with a trace warning rather than failing the read.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when no constructor of <typeparamref name="TObject"/>
    /// can be satisfied from the available columns.</exception>
    /// <exception cref="IndexOutOfRangeException">Thrown when a data line has fewer fields than the header row.</exception>
    public async Task<List<TObject>> ReadCsvAsync(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        var options = new CsvReaderOptions
        {
            Separator = GetCsvSeparator(),
            FormatProvider = CultureInfo,
            MissingField = CsvMissingFieldHandling.Throw
        };

        await using var csv = CsvReader.Create(new StreamReader(stream, leaveOpen: true), options);
        var records = new List<TObject>();

        await csv.ReadHeaderAsync(cancellationToken);
        var header = csv.Headers.ToArray();

        while (await csv.ReadAsync(cancellationToken))
        {
            var properties = new Dictionary<string, object?>();

            foreach (var headerItem in header)
            {
                var mapping = mappings.FirstOrDefault(m => m.ColumnName == headerItem);
                var propertyName = mapping?.PropertyName ?? headerItem;
                if (exclusions.Contains(propertyName))
                {
                    continue;
                }

                properties.TryAdd(propertyName, csv.GetString(headerItem));
            }

            var record = ObjectActivator.Activate<TObject>(properties);
            records.Add(record);
        }

        return records;
    }

    /// <summary>
    /// Writes objects to a CSV stream, emitting a header row followed by one line per object.
    /// </summary>
    /// <param name="stream">The stream that receives the CSV (UTF-8, no byte order mark). It is flushed and left
    /// open and, when seekable, rewound to where the CSV starts.</param>
    /// <param name="records">The objects to write, one per line, in enumeration order.</param>
    /// <returns>A task that completes once all records have been written.</returns>
    /// <remarks>
    /// Values are formatted using <see cref="CultureInfo"/>. Registered <see cref="AddMapping"/> mappings, when
    /// present, rename the corresponding column headers; otherwise property names are used. Properties passed to
    /// <see cref="Exclude"/> are left out.
    /// </remarks>
    public async Task WriteCsvAsync(Stream stream, IEnumerable<TObject> records)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(records);

        var options = new CsvWriterOptions
        {
            Separator = GetCsvSeparator(),
            FormatProvider = CultureInfo,
            Quoting = CsvQuoting.WhenNeededOrWhitespace
        };

        var start = stream.CanSeek ? stream.Position : 0;
        var properties = GetWrittenProperties();
        var header = properties.Select(GetColumnName).ToArray();

        await using (var csvWriter = CsvWriter.Create(new StreamWriter(stream, leaveOpen: true), options))
        {
            csvWriter.WriteHeader(header);

            foreach (var record in records)
            {
                foreach (var property in properties)
                {
                    csvWriter.WriteField(FormatCsvValue(property.GetValue(record)));
                }

                await csvWriter.EndRecordAsync();
            }

            await csvWriter.FlushAsync();
        }

        if (stream.CanSeek)
        {
            stream.Position = start;
        }
    }

    private string? FormatCsvValue(object? value) => value switch
    {
        null => null,
        string text => text,
        IFormattable formattable => formattable.ToString(null, CultureInfo),
        _ => value.ToString()
    };

    // CSV uses the culture's list separator (a comma for the invariant culture), falling back to a
    // comma when that separator is not a single ASCII character.
    private char GetCsvSeparator()
    {
        var listSeparator = CultureInfo.TextInfo.ListSeparator;
        return listSeparator.Length == 1 && char.IsAscii(listSeparator[0]) ? listSeparator[0] : ',';
    }

    #endregion

    private PropertyInfo[] GetWrittenProperties() =>
        typeof(TObject)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0 && !exclusions.Contains(p.Name))
            .ToArray();

    private string GetColumnName(PropertyInfo property) =>
        mappings.FirstOrDefault(m => m.PropertyName == property.Name)?.ColumnName ?? property.Name;

    private static MemberInfo GetExpressionMemberInfo(Expression<Func<TObject, object>> expression)
    {
        MemberExpression memberExpression;

        // Unwrap the expression if it's a unary expression
        if (expression.Body is UnaryExpression unaryExpression)
        {
            memberExpression = (MemberExpression)unaryExpression.Operand;
        }
        else
        {
            memberExpression = (MemberExpression)expression.Body;
        }

        // Retrieve the MemberInfo from the MemberExpression
        var memberInfo = memberExpression.Member;

        return memberInfo;
    }
}
