using NPOI.XSSF.UserModel;

namespace Halifax.Excel.Tests;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

public interface IPerson
{
    public string Name { get; }
    public int Age { get; }
    public string Company { get; }
}

class PersonClass : IPerson
{
    public PersonClass()
    {
    }
    
    public string Name { get; set; }
    public int Age { get; set; }
    public string Company { get; set; }
}

record PersonRecord : IPerson
{
    public string Name { get; set; }
    public int Age { get; set; }
    public string Company { get; set; }
}

record PersonRecordConstructorOnly(string Name, int Age, string Company) : IPerson;

record PersonRecordPartialConstructor : IPerson
{
    public PersonRecordPartialConstructor(string name)
    {
        Name = name;
    }

    public string Name { get; set; }
    public int Age { get; set; }
    public string Company { get; set; }
}

public class ExcelConverterTests
{
    private static readonly string tempDir = Path.GetTempPath();
    private static readonly string filenameExcel = Path.Combine(tempDir, "Export.xlsx");
    private static readonly string filenameCsv = Path.Combine(tempDir, "Export.csv");

    [SetUp]
    [TearDown]
    public void Setup()
    {
        if (File.Exists(filenameExcel)) File.Delete(filenameExcel);
        if (File.Exists(filenameCsv)) File.Delete(filenameCsv);
    }
    
    [Test]
    public async Task ReadWriteCsv_Class()
    {
        var converter = new ExcelConverter<PersonClass>();
        converter.AddMapping("Full Name", p => p.Name);
        converter.AddMapping("Age", p => p.Age);
        List<PersonClass> people =
        [
            new() { Name = "John Smith", Age = 34, Company = "ABC Co" },
            new() { Name = "John Doe", Age = 40, Company = "XYZ Company" }
        ];
        await ReadWriteCsvAsync(converter, people);
    }
    
    [Test]
    public async Task ReadWriteCsv_Record()
    {
        var converter = new ExcelConverter<PersonRecord>();
        converter.AddMapping("Full Name", p => p.Name);
        converter.AddMapping("Age", p => p.Age);
        List<PersonRecord> people =
        [
            new() { Name = "John Smith", Age = 34, Company = "ABC Co" },
            new() { Name = "John Doe", Age = 40, Company = "XYZ Company" }
        ];
        await ReadWriteCsvAsync(converter, people);
    }

    [Test]
    public async Task ReadWriteCsv_Record_ConstructorOnly()
    {
        var converter = new ExcelConverter<PersonRecordConstructorOnly>();
        converter.AddMapping("Full Name", p => p.Name);
        converter.AddMapping("Age", p => p.Age);
        List<PersonRecordConstructorOnly> people =
        [
            new("John Smith", 34, "ABC Co"),
            new("John Doe", 40, "XYZ Company")
        ];
        await ReadWriteCsvAsync(converter, people);
    }

    [Test]
    public async Task ReadWriteCsv_Record_PartialConstructor()
    {
        var converter = new ExcelConverter<PersonRecordPartialConstructor>();
        converter.AddMapping("Full Name", p => p.Name);
        converter.AddMapping("Age", p => p.Age);
        List<PersonRecordPartialConstructor> people =
        [
            new PersonRecordPartialConstructor("John Smith") { Age = 34, Company = "ABC Co" },
            new PersonRecordPartialConstructor("John Doe") { Age = 40, Company = "XYZ Company" }
        ];
        await ReadWriteCsvAsync(converter, people);
    }
    
    [Test]
    public async Task ReadWriteExcel_Class()
    {
        var converter = new ExcelConverter<PersonClass>();
        converter.AddMapping("Full Name", p => p.Name);
        converter.AddMapping("Age", p => p.Age);
        List<PersonClass> people =
        [
            new() { Name = "John Smith", Age = 34, Company = "ABC Co" },
            new() { Name = "John Doe", Age = 40, Company = "XYZ Company" }
        ];
        await ReadWriteExcelAsync(converter, people);
    }
    
    [Test]
    public async Task ReadWriteExcel_Record()
    {
        var converter = new ExcelConverter<PersonRecord>();
        converter.AddMapping("Full Name", p => p.Name);
        converter.AddMapping("Age", p => p.Age);
        List<PersonRecord> people =
        [
            new() { Name = "John Smith", Age = 34, Company = "ABC Co" },
            new() { Name = "John Doe", Age = 40, Company = "XYZ Company" }
        ];
        await ReadWriteExcelAsync(converter, people);
    }
    
    [Test]
    public async Task ReadWriteExcel_Record_ConstructorOnly()
    {
        var converter = new ExcelConverter<PersonRecordConstructorOnly>();
        converter.AddMapping("Full Name", p => p.Name);
        converter.AddMapping("Age", p => p.Age);
        List<PersonRecordConstructorOnly> people =
        [
            new("John Smith", 34, "ABC Co"),
            new("John Doe", 40, "XYZ Company")
        ];
        await ReadWriteExcelAsync(converter, people);
    }

    private static async Task ReadWriteExcelAsync<TObject>(
        ExcelConverter<TObject> converter, 
        List<TObject> source) where TObject : IPerson
    {
        using var stream = new MemoryStream();
        await converter.WriteExcelAsync(stream, source);
        await File.WriteAllBytesAsync(filenameExcel, stream.ToArray());
        await using var readStream = File.OpenRead(filenameExcel);
        var records = converter.ReadExcel(readStream);
        CompareLists(source, records);
    }

    private static async Task ReadWriteCsvAsync<TObject>(
        ExcelConverter<TObject> converter, 
        List<TObject> source) where TObject : IPerson
    {
        // Write
        using var stream = new MemoryStream();
        await converter.WriteCsvAsync(stream, source);
        await File.WriteAllBytesAsync(filenameCsv, stream.ToArray());
        
        // Read
        await using var readStream = File.OpenRead(filenameCsv);
        var records = await converter.ReadCsvAsync(readStream);
        CompareLists(source, records);
    }
    
    private static void CompareLists<TObject>(List<TObject> left, List<TObject> right) where TObject : IPerson
    {
        if (left.Count != right.Count) throw new Exception($"List count mismatch: left={left.Count}, right={right.Count}");

        for (var i = 0; i < left.Count; i++)
        {
            var l = left[i];
            var r = right[i];
            if (l.Name != r.Name || l.Age != r.Age || l.Company != r.Company)
                throw new Exception($"Difference at index {i}: Left({l.Name}, {l.Age}, {l.Company}) vs Right({r.Name}, {r.Age}, {r.Company})");
        }
    }
}

#pragma warning disable CS8618

class NumericModel
{
    public int IntValue { get; set; }
    public long LongValue { get; set; }
    public double DoubleValue { get; set; }
    public float FloatValue { get; set; }
    public decimal DecimalValue { get; set; }
    public short ShortValue { get; set; }
    public byte ByteValue { get; set; }
    public bool BoolValue { get; set; }
}

class NullableModel
{
    public string? Name { get; set; }
    public int? Age { get; set; }
    public double? Score { get; set; }
}

enum Status { Active, Inactive, Pending }

class EnumModel
{
    public string Name { get; set; }
    public Status Status { get; set; }
}

public class ExcelConverterNumericTests
{
    [Test]
    public async Task WriteExcel_NumericTypes_PreservesValues()
    {
        var converter = new ExcelConverter<NumericModel>();
        List<NumericModel> records =
        [
            new()
            {
                IntValue = 42, LongValue = 9999999999L, DoubleValue = 3.14159,
                FloatValue = 2.71f, DecimalValue = 123.45m, ShortValue = 99,
                ByteValue = 255, BoolValue = true
            },
            new()
            {
                IntValue = -1, LongValue = 0, DoubleValue = 0.0,
                FloatValue = -1.5f, DecimalValue = 0m, ShortValue = -32000,
                ByteValue = 0, BoolValue = false
            }
        ];

        var writeStream = new MemoryStream();
        await converter.WriteExcelAsync(writeStream, records);
        using var readStream = new MemoryStream(writeStream.ToArray());
        var result = converter.ReadExcel(readStream);

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0].IntValue, Is.EqualTo(42));
        Assert.That(result[0].LongValue, Is.EqualTo(9999999999L));
        Assert.That(result[0].DoubleValue, Is.EqualTo(3.14159).Within(0.0001));
        Assert.That(result[0].BoolValue, Is.True);
        Assert.That(result[1].IntValue, Is.EqualTo(-1));
        Assert.That(result[1].BoolValue, Is.False);
    }

    [Test]
    public async Task WriteCsv_NumericTypes_PreservesValues()
    {
        var converter = new ExcelConverter<NumericModel>();
        List<NumericModel> records =
        [
            new()
            {
                IntValue = 42, LongValue = 9999999999L, DoubleValue = 3.14159,
                FloatValue = 2.71f, DecimalValue = 123.45m, ShortValue = 99,
                ByteValue = 255, BoolValue = true
            }
        ];

        var writeStream = new MemoryStream();
        await converter.WriteCsvAsync(writeStream, records);
        using var readStream = new MemoryStream(writeStream.ToArray());
        var result = await converter.ReadCsvAsync(readStream);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].IntValue, Is.EqualTo(42));
        Assert.That(result[0].LongValue, Is.EqualTo(9999999999L));
        Assert.That(result[0].DoubleValue, Is.EqualTo(3.14159).Within(0.0001));
        Assert.That(result[0].BoolValue, Is.True);
    }
}

public class ExcelConverterEmptyTests
{
    [Test]
    public async Task WriteExcel_EmptyList_ProducesValidFile()
    {
        var converter = new ExcelConverter<PersonClass>();
        var writeStream = new MemoryStream();
        await converter.WriteExcelAsync(writeStream, []);
        using var readStream = new MemoryStream(writeStream.ToArray());
        var result = converter.ReadExcel(readStream);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task WriteCsv_EmptyList_ProducesValidFile()
    {
        var converter = new ExcelConverter<PersonClass>();
        var writeStream = new MemoryStream();
        await converter.WriteCsvAsync(writeStream, []);
        using var readStream = new MemoryStream(writeStream.ToArray());
        var result = await converter.ReadCsvAsync(readStream);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task WriteExcel_SingleRecord_RoundTrips()
    {
        var converter = new ExcelConverter<PersonClass>();
        List<PersonClass> records = [new() { Name = "Solo", Age = 1, Company = "Only" }];

        var writeStream = new MemoryStream();
        await converter.WriteExcelAsync(writeStream, records);
        using var readStream = new MemoryStream(writeStream.ToArray());
        var result = converter.ReadExcel(readStream);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("Solo"));
    }
}

public class ExcelConverterMappingTests
{
    [Test]
    public async Task WriteExcel_NoMappings_UsesPropertyNames()
    {
        var converter = new ExcelConverter<PersonClass>();
        List<PersonClass> records =
        [
            new() { Name = "Alice", Age = 30, Company = "Tech" }
        ];

        var writeStream = new MemoryStream();
        await converter.WriteExcelAsync(writeStream, records);

        using var workbook = new XSSFWorkbook(new MemoryStream(writeStream.ToArray()));
        var sheet = workbook.GetSheetAt(0);
        var header = sheet.GetRow(0);

        Assert.That(header.GetCell(0).StringCellValue, Is.EqualTo("Name"));
        Assert.That(header.GetCell(1).StringCellValue, Is.EqualTo("Age"));
        Assert.That(header.GetCell(2).StringCellValue, Is.EqualTo("Company"));
    }

    [Test]
    public async Task WriteExcel_WithMappings_UsesCustomColumnNames()
    {
        var converter = new ExcelConverter<PersonClass>();
        converter.AddMapping("Full Name", p => p.Name);
        converter.AddMapping("Years", p => p.Age);
        List<PersonClass> records =
        [
            new() { Name = "Alice", Age = 30, Company = "Tech" }
        ];

        var writeStream = new MemoryStream();
        await converter.WriteExcelAsync(writeStream, records);

        using var workbook = new XSSFWorkbook(new MemoryStream(writeStream.ToArray()));
        var sheet = workbook.GetSheetAt(0);
        var header = sheet.GetRow(0);

        Assert.That(header.GetCell(0).StringCellValue, Is.EqualTo("Full Name"));
        Assert.That(header.GetCell(1).StringCellValue, Is.EqualTo("Years"));
        Assert.That(header.GetCell(2).StringCellValue, Is.EqualTo("Company"));
    }

    [Test]
    public async Task WriteCsv_NoMappings_RoundTrips()
    {
        var converter = new ExcelConverter<PersonClass>();
        List<PersonClass> records =
        [
            new() { Name = "Bob", Age = 25, Company = "Corp" }
        ];

        var writeStream = new MemoryStream();
        await converter.WriteCsvAsync(writeStream, records);
        using var readStream = new MemoryStream(writeStream.ToArray());
        var result = await converter.ReadCsvAsync(readStream);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("Bob"));
        Assert.That(result[0].Age, Is.EqualTo(25));
        Assert.That(result[0].Company, Is.EqualTo("Corp"));
    }
}

public class ExcelConverterReadAsyncTests
{
    [Test]
    public async Task ReadAsync_ExcelContentType_ReadsExcel()
    {
        var converter = new ExcelConverter<PersonClass>();
        List<PersonClass> records = [new() { Name = "Test", Age = 1, Company = "Co" }];

        using var writeStream = new MemoryStream();
        await converter.WriteExcelAsync(writeStream, records);
        var bytes = writeStream.ToArray();

        using var readStream = new MemoryStream(bytes);
        var result = await converter.ReadAsync(readStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("Test"));
    }

    [Test]
    public async Task ReadAsync_CsvContentType_ReadsCsv()
    {
        var converter = new ExcelConverter<PersonClass>();
        List<PersonClass> records = [new() { Name = "Test", Age = 1, Company = "Co" }];

        using var writeStream = new MemoryStream();
        await converter.WriteCsvAsync(writeStream, records);
        var bytes = writeStream.ToArray();

        using var readStream = new MemoryStream(bytes);
        var result = await converter.ReadAsync(readStream, "text/csv");

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("Test"));
    }

    [Test]
    public void ReadAsync_UnsupportedContentType_Throws()
    {
        var converter = new ExcelConverter<PersonClass>();
        using var stream = new MemoryStream();
        Assert.ThrowsAsync<NotSupportedException>(() =>
            converter.ReadAsync(stream, "application/json"));
    }

    [Test]
    public void ReadAsync_NullStream_Throws()
    {
        var converter = new ExcelConverter<PersonClass>();
        Assert.ThrowsAsync<ArgumentNullException>(() =>
            converter.ReadAsync(null!, "text/csv"));
    }

    [Test]
    public void ReadAsync_NullContentType_Throws()
    {
        var converter = new ExcelConverter<PersonClass>();
        using var stream = new MemoryStream();
        Assert.ThrowsAsync<ArgumentNullException>(() =>
            converter.ReadAsync(stream, null!));
    }

    [Test]
    public async Task ReadAsync_LegacyExcelContentType_ReadsExcel()
    {
        var converter = new ExcelConverter<PersonClass>();
        List<PersonClass> records = [new() { Name = "Test", Age = 1, Company = "Co" }];

        using var writeStream = new MemoryStream();
        await converter.WriteExcelAsync(writeStream, records);
        var bytes = writeStream.ToArray();

        using var readStream = new MemoryStream(bytes);
        var result = await converter.ReadAsync(readStream, "application/vnd.ms-excel");

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("Test"));
    }
}

public class ExcelConverterNullableTests
{
    [Test]
    public async Task WriteCsv_NullableProperties_RoundTrips()
    {
        var converter = new ExcelConverter<NullableModel>();
        List<NullableModel> records =
        [
            new() { Name = "Alice", Age = 30, Score = 95.5 },
            new() { Name = null, Age = null, Score = null }
        ];

        var writeStream = new MemoryStream();
        await converter.WriteCsvAsync(writeStream, records);
        using var readStream = new MemoryStream(writeStream.ToArray());
        var result = await converter.ReadCsvAsync(readStream);

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0].Name, Is.EqualTo("Alice"));
        Assert.That(result[0].Age, Is.EqualTo(30));
        Assert.That(result[1].Name, Is.Null.Or.Empty);
    }

    [Test]
    public async Task WriteExcel_NullValues_DoesNotThrow()
    {
        var converter = new ExcelConverter<NullableModel>();
        List<NullableModel> records =
        [
            new() { Name = "Alice", Age = null, Score = null }
        ];

        var writeStream = new MemoryStream();
        await converter.WriteExcelAsync(writeStream, records);
        using var readStream = new MemoryStream(writeStream.ToArray());
        var result = converter.ReadExcel(readStream);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("Alice"));
        Assert.That(result[0].Age, Is.Null);
        Assert.That(result[0].Score, Is.Null);
    }
}

public class ExcelConverterEnumTests
{
    [Test]
    public async Task WriteCsv_EnumProperty_RoundTrips()
    {
        var converter = new ExcelConverter<EnumModel>();
        List<EnumModel> records =
        [
            new() { Name = "Alice", Status = Status.Active },
            new() { Name = "Bob", Status = Status.Pending }
        ];

        var writeStream = new MemoryStream();
        await converter.WriteCsvAsync(writeStream, records);
        using var readStream = new MemoryStream(writeStream.ToArray());
        var result = await converter.ReadCsvAsync(readStream);

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0].Status, Is.EqualTo(Status.Active));
        Assert.That(result[1].Status, Is.EqualTo(Status.Pending));
    }
}

public class ExcelConverterCustomSheetTests
{
    [Test]
    public async Task WriteExcel_CustomSheetName()
    {
        var converter = new ExcelConverter<PersonClass>();
        List<PersonClass> records = [new() { Name = "Test", Age = 1, Company = "Co" }];

        var writeStream = new MemoryStream();
        await converter.WriteExcelAsync(writeStream, records, "MySheet");

        using var workbook = new XSSFWorkbook(new MemoryStream(writeStream.ToArray()));
        Assert.That(workbook.GetSheetName(0), Is.EqualTo("MySheet"));
    }

    [Test]
    public async Task WriteExcel_LargeDataset_Succeeds()
    {
        var converter = new ExcelConverter<PersonClass>();
        var records = Enumerable.Range(0, 1000)
            .Select(i => new PersonClass { Name = $"Person {i}", Age = i, Company = $"Company {i}" })
            .ToList();

        var writeStream = new MemoryStream();
        await converter.WriteExcelAsync(writeStream, records);
        using var readStream = new MemoryStream(writeStream.ToArray());
        var result = converter.ReadExcel(readStream);

        Assert.That(result, Has.Count.EqualTo(1000));
        Assert.That(result[0].Name, Is.EqualTo("Person 0"));
        Assert.That(result[999].Name, Is.EqualTo("Person 999"));
    }
}