using System.Globalization;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using CsvHelper;
using Ganss.Excel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace Halifax.Excel;

public class ExcelConverter<TObject>
{
    private const int defaultWidthToStringLengthFactor = 300;
    public int WidthToStringLengthFactor { get; set; } = defaultWidthToStringLengthFactor;
    public int MaxCellWidth { get; set; } = 30 * defaultWidthToStringLengthFactor;
    public int MinCellWidth { get; set; } = 9 * defaultWidthToStringLengthFactor;

    public CultureInfo CultureInfo { get; set; } = CultureInfo.InvariantCulture;
    public bool HasHeader { get; set; } = true;

    private readonly List<ColumnMapping<TObject>> mappings = new();

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
    
    public async Task<List<TObject>> ReadAsync(
        Stream stream, 
        string contentType, 
        CancellationToken cancellationToken = default)
    {
        stream = stream ?? throw new ArgumentNullException(nameof(stream));
        contentType = (contentType ?? throw new ArgumentNullException(nameof(contentType))).ToLower();

        if (contentType.Contains("vnd") || contentType.Contains("office") || contentType.Contains("excel"))
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

    public List<TObject> ReadExcel(Stream stream)
    {
        var excel = new ExcelMapper(stream) {HeaderRow = HasHeader};
        var records = excel.Fetch<TObject>().ToList();
        return records;
    }
    
    public Task WriteExcelAsync(Stream stream, IEnumerable<TObject> records, string sheetName = "Sheet 0")
    {
        using var workbook = new XSSFWorkbook();
        var sheet = workbook.CreateSheet(sheetName);
        var headerStyle = CreateHeaderStyle(workbook);
        var properties = typeof(TObject).GetProperties().ToList();
        var rowIndex = 0;
        var valueSets = records.Select(r => properties.Select(p => Convert.ToString(p.GetValue(r))).ToList()).ToList();
        
        if (HasHeader)
        {
            var row = sheet.CreateRow(rowIndex++);
            for (var colIndex = 0; colIndex < properties.Count; colIndex++)
            {
                var cell = row.CreateCell(colIndex);
                var property = properties[colIndex];
                var columnMapping = mappings.FirstOrDefault(m => m.PropertyName == property.Name);
                var value = columnMapping?.ColumnName ?? property.Name;
                
                cell.SetCellValue(value);
                cell.CellStyle = headerStyle;

                var maxLength = valueSets.Select(set => set[colIndex].Length).Max();
                var width = Math.Max(maxLength*WidthToStringLengthFactor, value.Length*WidthToStringLengthFactor);
                if (MinCellWidth > 0) width = Math.Max(width, MinCellWidth);
                if (MaxCellWidth > 0) width = Math.Min(width, MaxCellWidth);
                sheet.SetColumnWidth(cell.ColumnIndex, width);
            }
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
            
            case DateOnly valueDateOnly:
                cell.SetCellValue(valueDateOnly);
                break;
            
            case DateTime valueDateTime:
                cell.SetCellValue(valueDateTime);
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

        // Backgrounds don't work properly :/
        // style.FillBackgroundColor = IndexedColors.LightYellow.Index;
        // style.FillPattern = FillPattern.SolidForeground;
        
        return style;
    }
    
    #endregion

    #region CSV

    public async Task<List<TObject>> ReadCsvAsync(
        Stream stream, 
        CancellationToken cancellationToken = default)
    {
        using var streamReader = new StreamReader(stream);
        using var csv = new CsvReader(streamReader, CultureInfo);
        ConfigureCsvContext(csv.Context);
        var results = new List<TObject>();
        
        await foreach (var chunk in csv.GetRecordsAsync<TObject>(cancellationToken))
        {
            results.Add(chunk);
        }

        return results;
    }

    public async Task WriteCsvAsync(Stream stream, IEnumerable<TObject> records)
    {
        await using var writer = new StreamWriter(stream);
        await using var csvWriter = new CsvWriter(writer, CultureInfo);
    
        ConfigureCsvContext(csvWriter.Context);
        await csvWriter.WriteRecordsAsync(records);
    }

    private void ConfigureCsvContext(CsvContext context)
    {
        context.Configuration.HasHeaderRecord = HasHeader;
        
        if (mappings.Any())
        {
            var map = context.AutoMap<TObject>();
            
            foreach (var mapping in mappings)
            {
                map.Map(mapping.Expression).Name(mapping.ColumnName);
            }

            context.RegisterClassMap(map);
        }
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