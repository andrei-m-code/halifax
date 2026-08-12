using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using CsvHelper;
using CsvHelper.Configuration;
using Ganss.Excel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace Halifax.Excel;

/// <summary>
/// Converts between collections of strongly-typed objects and Excel (.xlsx) or CSV files.
/// Supports custom column-name mappings and automatic conversion between cell values and property types.
/// </summary>
/// <typeparam name="TObject">The type of object to read from or write to Excel/CSV. When reading,
/// values are materialized onto this type; when writing, its public properties become columns.</typeparam>
/// <remarks>
/// A single converter instance can be reused across multiple read and write calls. Register any
/// custom header-to-property mappings with <see cref="AddMapping"/> before calling a read or write
/// method; unmapped properties fall back to the property name as the column header.
/// </remarks>
/// <example>
/// <code>
/// var converter = new ExcelConverter&lt;Person&gt;();
/// converter.AddMapping("Full Name", p =&gt; p.Name);
///
/// // Write to a stream as Excel
/// await converter.WriteExcelAsync(outputStream, people);
///
/// // Read back, auto-detecting the format from the content type
/// var loaded = await converter.ReadAsync(inputStream, "text/csv");
/// </code>
/// </example>
public class ExcelConverter<TObject>
{
    private const int defaultWidthToStringLengthFactor = 300;

    /// <summary>
    /// Multiplier applied to the longest cell/header text length (in characters) when sizing each
    /// Excel column during <see cref="WriteExcelAsync"/>. Higher values produce wider columns. Default is 300.
    /// </summary>
    public int WidthToStringLengthFactor { get; set; } = defaultWidthToStringLengthFactor;

    /// <summary>
    /// Upper bound, in Excel width units, applied to every column written by <see cref="WriteExcelAsync"/>.
    /// A computed width larger than this is clamped down. Ignored when set to zero or less. Default is 9000 (30 * 300).
    /// </summary>
    public int MaxCellWidth { get; set; } = 30 * defaultWidthToStringLengthFactor;

    /// <summary>
    /// Lower bound, in Excel width units, applied to every column written by <see cref="WriteExcelAsync"/>.
    /// A computed width smaller than this is clamped up. Ignored when set to zero or less. Default is 2700 (9 * 300).
    /// </summary>
    public int MinCellWidth { get; set; } = 9 * defaultWidthToStringLengthFactor;

    /// <summary>
    /// Culture used to parse and format values when reading and writing CSV (via <see cref="ReadCsvAsync"/>
    /// and <see cref="WriteCsvAsync"/>). Excel reads and writes are not affected by this setting.
    /// Default is <see cref="CultureInfo.InvariantCulture"/>.
    /// </summary>
    public CultureInfo CultureInfo { get; set; } = CultureInfo.InvariantCulture;

    private readonly List<ColumnMapping<TObject>> mappings = [];

    /// <summary>
    /// Registers a mapping between a file column header and a property of <typeparamref name="TObject"/>.
    /// The mapping is applied by every subsequent read and write on this converter to translate
    /// between column headers and object properties.
    /// </summary>
    /// <param name="columnName">The column header text as it appears (or should appear) in the Excel or CSV file.</param>
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
    /// Reads objects from a stream, choosing the Excel or CSV reader based on the supplied content type.
    /// </summary>
    /// <param name="stream">The stream to read from. The data must begin with a header row.</param>
    /// <param name="contentType">The MIME content type used to select a reader. Matching is case-insensitive:
    /// values containing <c>application/vnd.openxmlformats-officedocument</c> or <c>application/vnd.ms-excel</c>
    /// are read as Excel (see <see cref="ReadExcel"/>); values containing <c>csv</c> are read as CSV
    /// (see <see cref="ReadCsvAsync"/>).</param>
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
    /// Reads objects from an Excel (.xlsx) stream. The first row is treated as the header row.
    /// </summary>
    /// <param name="stream">The Excel stream to read from.</param>
    /// <returns>The deserialized objects, one per data row.</returns>
    /// <remarks>Columns are matched to properties by header text. Registered <see cref="AddMapping"/>
    /// mappings, when present, override the default header-to-property matching.</remarks>
    public List<TObject> ReadExcel(Stream stream)
    {
        var excel = new ExcelMapper(stream) {HeaderRow = true};

        if (mappings.Count > 0)
        {
            foreach (var mapping in mappings)
            {
                excel.AddMapping(mapping.ColumnName, mapping.Expression);
            }
        }

        var records = excel.Fetch<TObject>().ToList();
        return records;
    }

    /// <summary>
    /// Writes objects to an Excel (.xlsx) stream as a single worksheet with a styled header row and sized columns.
    /// </summary>
    /// <param name="stream">The stream that receives the workbook. It is closed once the workbook has been written.</param>
    /// <param name="records">The objects to write, one per row, in enumeration order.</param>
    /// <param name="sheetName">The worksheet name. Default is "Sheet 0".</param>
    /// <returns>A completed task; the work is performed synchronously.</returns>
    /// <remarks>
    /// One column is emitted per public property of <typeparamref name="TObject"/>, in reflection order. Header cells
    /// use the mapped column name from <see cref="AddMapping"/> when one exists, otherwise the property name, and are
    /// rendered bold on a light-yellow fill. Column widths are derived from the widest value using
    /// <see cref="WidthToStringLengthFactor"/> and clamped to <see cref="MinCellWidth"/>/<see cref="MaxCellWidth"/>.
    /// Cell values are written in their native Excel type for <see cref="bool"/>, <see cref="string"/>,
    /// <see cref="DateTime"/>, <see cref="DateTimeOffset"/> (as its <see cref="DateTimeOffset.DateTime"/>),
    /// <see cref="byte"/>, <see cref="short"/>, <see cref="int"/>, <see cref="long"/>, <see cref="float"/>,
    /// <see cref="double"/> and <see cref="decimal"/> (converted to <see cref="double"/>); any other type is written
    /// as its string representation. <see langword="null"/> values leave the cell empty.
    /// </remarks>
    public Task WriteExcelAsync(Stream stream, IEnumerable<TObject> records, string sheetName = "Sheet 0")
    {
        using var workbook = new XSSFWorkbook();
        var sheet = workbook.CreateSheet(sheetName);
        var headerStyle = CreateHeaderStyle(workbook);
        var properties = typeof(TObject).GetProperties().ToList();
        var rowIndex = 0;
        var valueSets = records.Select(r => properties.Select(p => Convert.ToString(p.GetValue(r))).ToList()).ToList();

        var headerRow = sheet.CreateRow(rowIndex++);
        for (var colIndex = 0; colIndex < properties.Count; colIndex++)
        {
            var cell = headerRow.CreateCell(colIndex);
            var property = properties[colIndex];
            var columnMapping = mappings.FirstOrDefault(m => m.PropertyName == property.Name);
            var value = columnMapping?.ColumnName ?? property.Name;

            cell.SetCellValue(value);
            cell.CellStyle = headerStyle;

            var maxLength = valueSets.Count > 0
                ? valueSets.Select(set => (set[colIndex] ?? string.Empty).Length).Max()
                : 0;
            var width = Math.Max(maxLength*WidthToStringLengthFactor, value.Length*WidthToStringLengthFactor);
            if (MinCellWidth > 0) width = Math.Max(width, MinCellWidth);
            if (MaxCellWidth > 0) width = Math.Min(width, MaxCellWidth);
            sheet.SetColumnWidth(cell.ColumnIndex, width);
        }

        foreach (var record in records)
        {
            var row = sheet.CreateRow(rowIndex++);
            for (var propertyIndex = 0; propertyIndex < properties.Count; propertyIndex++)
            {
                var property = properties[propertyIndex];
                var cell = row.CreateCell(propertyIndex);
                SetCellValue(property, record, cell);
            }
        }

        workbook.Write(stream);

        return Task.CompletedTask;
    }

    private static void SetCellValue(PropertyInfo propertyInfo, TObject record, ICell cell)
    {
        var value = propertyInfo.GetValue(record);

        if (value == null)
        {
            return;
        }

        switch (value)
        {
            case bool valueBool:
                cell.SetCellValue(valueBool);
                break;

            case string valueString:
                cell.SetCellValue(valueString);
                break;

            case DateTime valueDateTime:
                cell.SetCellValue(valueDateTime);
                break;

            case DateTimeOffset valueDateTimeOffset:
                cell.SetCellValue(valueDateTimeOffset.DateTime);
                break;

            case byte valueByte:
                cell.SetCellValue(valueByte);
                break;

            case int valueInt:
                cell.SetCellValue(valueInt);
                break;

            case long valueLong:
                cell.SetCellValue(valueLong);
                break;

            case double valueDouble:
                cell.SetCellValue(valueDouble);
                break;

            case float valueFloat:
                cell.SetCellValue(valueFloat);
                break;

            case decimal valueDecimal:
                var convertedDecimal = Convert.ToDouble(valueDecimal);
                cell.SetCellValue(convertedDecimal);
                break;

            case short valueShort:
                cell.SetCellValue(valueShort);
                break;

            default:
                cell.SetCellValue(Convert.ToString(value));
                break;
        }
    }

    private static ICellStyle CreateHeaderStyle(IWorkbook workbook)
    {
        var style = workbook.CreateCellStyle();
        var font = workbook.CreateFont();
        font.IsBold = true;
        style.SetFont(font);

        style.FillForegroundColor = IndexedColors.LightYellow.Index;
        style.FillPattern = FillPattern.SolidForeground;

        return style;
    }

    #endregion

    #region CSV

    /// <summary>
    /// Reads objects from a CSV stream. The first line is treated as the header row and is used to map
    /// each subsequent line onto a new <typeparamref name="TObject"/>.
    /// </summary>
    /// <param name="stream">The CSV stream to read from, parsed with <see cref="CultureInfo"/>.</param>
    /// <param name="cancellationToken">Token that cancels the read.</param>
    /// <returns>The deserialized objects, one per data line.</returns>
    /// <remarks>
    /// Header text is translated to a property name via registered <see cref="AddMapping"/> mappings, falling back
    /// to the header text itself. Each row is materialized with <see cref="ObjectActivator"/>, which selects the
    /// constructor best matching the available columns and then sets any remaining writable public properties;
    /// individual values that cannot be converted to the target type are skipped with a trace warning rather than
    /// failing the read.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when no constructor of <typeparamref name="TObject"/>
    /// can be satisfied from the available columns.</exception>
    public async Task<List<TObject>> ReadCsvAsync(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        using var streamReader = new StreamReader(stream);
        using var csv = new CsvReader(streamReader, CultureInfo);

        ConfigureCsvContext(csv.Context);
        var records = new List<TObject>();

        await csv.ReadAsync();
        csv.ReadHeader();
        var header = csv.HeaderRecord!;

        while (await csv.ReadAsync())
        {
            var properties = new Dictionary<string, object>();

            foreach (var headerItem in header)
            {
                var mapping = mappings.FirstOrDefault(m => m.ColumnName == headerItem);
                var propertyName = mapping?.PropertyName ?? headerItem;
                properties.Add(propertyName, csv[headerItem]!);
            }

            var record = ObjectActivator.Activate<TObject>(properties);
            records.Add(record);
        }

        return records;
    }

    /// <summary>
    /// Writes objects to a CSV stream, emitting a header row followed by one line per object.
    /// </summary>
    /// <param name="stream">The stream that receives the CSV. It is flushed and disposed when writing completes.</param>
    /// <param name="records">The objects to write, one per line, in enumeration order.</param>
    /// <returns>A task that completes once all records have been written.</returns>
    /// <remarks>
    /// Values are formatted using <see cref="CultureInfo"/>. Registered <see cref="AddMapping"/> mappings, when
    /// present, rename the corresponding column headers; otherwise property names are used.
    /// </remarks>
    public async Task WriteCsvAsync(Stream stream, IEnumerable<TObject> records)
    {
        await using var writer = new StreamWriter(stream);
        await using var csvWriter = new CsvWriter(writer, CultureInfo);
        ConfigureCsvContext(csvWriter.Context);
        await csvWriter.WriteRecordsAsync(records);
    }

    private void ConfigureCsvContext(CsvContext context)
    {
        context.Configuration.HasHeaderRecord = true;
        if (mappings.Count == 0) return;

        var map = new DefaultClassMap<TObject>();
        map.AutoMap(context.Configuration);

        foreach (var mapping in mappings)
        {
            map.Map((Expression<Func<TObject, object?>>)(object)mapping.Expression).Name(mapping.ColumnName);
        }

        context.RegisterClassMap(map);
    }

    #endregion

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
