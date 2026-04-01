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
/// Converts between strongly-typed objects and Excel (.xlsx) or CSV files.
/// Supports custom column name mappings and automatic type conversion.
/// </summary>
/// <typeparam name="TObject">The type of object to read from or write to Excel/CSV.</typeparam>
public class ExcelConverter<TObject>
{
    private const int defaultWidthToStringLengthFactor = 300;

    /// <summary>
    /// Multiplier used to calculate column width from string length. Default is 300.
    /// </summary>
    public int WidthToStringLengthFactor { get; set; } = defaultWidthToStringLengthFactor;

    /// <summary>
    /// Maximum column width in Excel output. Default is 9000 (30 * 300).
    /// </summary>
    public int MaxCellWidth { get; set; } = 30 * defaultWidthToStringLengthFactor;

    /// <summary>
    /// Minimum column width in Excel output. Default is 2700 (9 * 300).
    /// </summary>
    public int MinCellWidth { get; set; } = 9 * defaultWidthToStringLengthFactor;

    /// <summary>
    /// Culture used for CSV parsing and writing. Default is <see cref="CultureInfo.InvariantCulture"/>.
    /// </summary>
    public CultureInfo CultureInfo { get; set; } = CultureInfo.InvariantCulture;

    private readonly List<ColumnMapping<TObject>> mappings = [];

    /// <summary>
    /// Maps a custom column name to a property on <typeparamref name="TObject"/>.
    /// Used during both reading and writing to translate between column headers and object properties.
    /// </summary>
    /// <param name="columnName">The column name in the Excel or CSV file.</param>
    /// <param name="propertyExpression">A lambda expression selecting the property to map.</param>
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
    /// Reads objects from a stream, automatically selecting Excel or CSV format based on the content type.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="contentType">The MIME content type (e.g. "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" or "text/csv").</param>
    /// <param name="cancellationToken">Cancellation token for CSV reading.</param>
    /// <returns>A list of deserialized objects.</returns>
    /// <exception cref="NotSupportedException">Thrown when the content type is not recognized.</exception>
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
    /// Reads objects from an Excel (.xlsx) stream. The stream must contain a header row.
    /// </summary>
    /// <param name="stream">The Excel stream to read from.</param>
    /// <returns>A list of deserialized objects.</returns>
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
    /// Writes objects to an Excel (.xlsx) stream with a bold header row and auto-sized columns.
    /// The stream will be closed after writing.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="records">The objects to write as rows.</param>
    /// <param name="sheetName">The name of the worksheet. Default is "Sheet 0".</param>
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
    /// Reads objects from a CSV stream. The CSV must contain a header row.
    /// Column name mappings are applied to translate headers to property names.
    /// </summary>
    /// <param name="stream">The CSV stream to read from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of deserialized objects.</returns>
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
    /// Writes objects to a CSV stream with a header row.
    /// The stream will be closed after writing.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="records">The objects to write as rows.</param>
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
