using CsvHelper.Configuration;

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

record PersonRecordPartialConstructor(string name) : IPerson
{
    public string Name { get; set; } = name;
    public int Age { get; set; }
    public string Company { get; set; }
}

public class ExcelConverterTests
{
    private static readonly string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
    private static readonly string filenameExcel = Path.Combine(desktop, "Export.xlsx");
    private static readonly string filenameCsv = Path.Combine(desktop, "Export.csv");

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
    public void ReadWriteCsv_Record_ConstructorOnly()
    {
        var converter = new ExcelConverter<PersonRecordConstructorOnly>();
        converter.AddMapping("Full Name", p => p.Name);
        converter.AddMapping("Age", p => p.Age);
        List<PersonRecordConstructorOnly> people =
        [
            new("John Smith", 34, "ABC Co"),
            new("John Doe", 40, "XYZ Company")
        ];
    }
    
    [Test]
    public Task ReadWriteCsv_Record_PartialConstructor()
    {
        var converter = new ExcelConverter<PersonRecordPartialConstructor>();
        converter.AddMapping("Full Name", p => p.Name);
        converter.AddMapping("Age", p => p.Age);
        List<PersonRecordConstructorOnly> people =
        [
            new("John Smith", 34, "ABC Co"),
            new("John Doe", 40, "XYZ Company")
        ];
        return Task.CompletedTask;
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
    
    private static ExcelConverter<TObject> CreateConverter<TObject>() where TObject : IPerson
    {
        var converter = new ExcelConverter<TObject>();
        converter.AddMapping("Full Name", p => p.Name);
        converter.AddMapping("Age", p => p.Age);
        return converter;
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