using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using CsvHelper;
using Ganss.Excel;

namespace Halifax.Excel;

public class ExcelConverter<TObject>
{
    public CultureInfo CultureInfo { get; set; } = CultureInfo.InvariantCulture;
    public bool HasHeader { get; set; } = true;

    private readonly List<ColumnMapping<TObject>> Mappings = new();

    public void AddMapping(string columnName, Expression<Func<TObject, object>> propertyExpression)
    {
        var memberInfo = GetExpressionMemberInfo(propertyExpression);
        Mappings.Add(new ColumnMapping<TObject>
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
        var excel = GetMapper(stream);
        var records = excel.Fetch<TObject>().ToList();
        return records;
    }
    
    public async Task<MemoryStream> WriteExcelAsync(IEnumerable<TObject> records)
    {
        var memoryStream = new MemoryStream();
        var excel = GetMapper();
        await excel.SaveAsync(memoryStream, records);
        
        return memoryStream;
    }

    private ExcelMapper GetMapper(Stream stream = null)
    {
        var excel = stream == null ? new ExcelMapper() : new ExcelMapper(stream);
        excel.HeaderRow = HasHeader;

        if (Mappings.Any())
        {
            var typeMapper = excel.TypeMapperFactory.Create(typeof(TObject));
            typeMapper.ColumnsByName.Clear();
            var type = typeof(TObject);
            typeof(TObject).GetProperties().ToList().ForEach(property =>
            {
                var mapping = Mappings.FirstOrDefault(m => m.PropertyName == property.Name);
                if (mapping != null)
                {
                    excel.AddMapping(mapping.ColumnName, mapping.Expression);
                }
                else
                {
                    excel.AddMapping(type, property.Name, property.Name);    
                }
            });
            Mappings.ForEach(m => excel.AddMapping(m.ColumnName, m.Expression));
        }
        
        return excel;
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

    public async Task<MemoryStream> WriteCsvAsync<TObject>(IEnumerable<TObject> records)
    {
        var memoryStream = new MemoryStream();
        await using var writer = new StreamWriter(memoryStream);
        await using var csvWriter = new CsvWriter(writer, CultureInfo);
    
        ConfigureCsvContext(csvWriter.Context);
        await csvWriter.WriteRecordsAsync(records);
    
        return memoryStream;
    }

    private void ConfigureCsvContext(CsvContext context)
    {
        context.Configuration.HasHeaderRecord = HasHeader;
        
        if (Mappings.Any())
        {
            var map = context.AutoMap<TObject>();
            
            foreach (var mapping in Mappings)
            {
                map.Map(mapping.Expression).Name(mapping.ColumnName);
            }

            context.RegisterClassMap(map);
        }
    }
    
    #endregion
    
    private static MemberInfo GetExpressionMemberInfo<TObject>(Expression<Func<TObject, object>> expression)
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