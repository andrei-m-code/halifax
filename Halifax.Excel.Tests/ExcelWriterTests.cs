using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace Halifax.Excel.Tests;

#pragma warning disable CS8618

class DatedRow
{
    public string Name { get; set; }
    public DateTime When { get; set; }
    public DateOnly Day { get; set; }
    public DateOnly? OptionalDay { get; set; }
    public DateTimeOffset Stamp { get; set; }
}

class UnsignedRow
{
    public uint A { get; set; }
    public ulong B { get; set; }
    public ushort C { get; set; }
    public sbyte D { get; set; }
}

class StaticPropertyRow
{
    public static string Shared { get; set; } = "not a column";
    public string Name { get; set; }
}

class SearchRow
{
    public string Search { get; set; }
    public string CustomerName { get; set; }
    public DateTime Date { get; set; }
}

/// <summary>A stream that cannot seek, like an HTTP response body.</summary>
class ForwardOnlyStream(Stream inner) : Stream
{
    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override void Flush() => inner.Flush();
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => inner.Write(buffer, offset, count);
}

public class ExcelWriterStreamTests
{
    private static readonly List<PersonClass> people = [new() { Name = "Alice", Age = 30, Company = "Tech" }];

    [Test]
    public async Task WriteExcel_LeavesTheStreamOpenAndRewound()
    {
        var stream = new MemoryStream();

        await new ExcelConverter<PersonClass>().WriteExcelAsync(stream, people);

        Assert.That(stream.CanRead, Is.True);
        Assert.That(stream.Position, Is.Zero);
        using var workbook = new XSSFWorkbook(stream);
        Assert.That(workbook.GetSheetAt(0).GetRow(1).GetCell(0).StringCellValue, Is.EqualTo("Alice"));
    }

    [Test]
    public async Task WriteExcel_RewindsToWhereTheWorkbookStarts()
    {
        var stream = new MemoryStream();
        stream.Write([1, 2, 3]);

        await new ExcelConverter<PersonClass>().WriteExcelAsync(stream, people);

        Assert.That(stream.Position, Is.EqualTo(3));
        var records = new ExcelConverter<PersonClass>().ReadExcel(new MemoryStream(stream.ToArray()[3..]));
        Assert.That(records.Single().Name, Is.EqualTo("Alice"));
    }

    [Test]
    public async Task WriteExcel_ToAForwardOnlyStream_Works()
    {
        var target = new MemoryStream();

        await new ExcelConverter<PersonClass>().WriteExcelAsync(new ForwardOnlyStream(target), people);

        Assert.That(new ExcelConverter<PersonClass>().ReadExcel(new MemoryStream(target.ToArray())).Single().Age, Is.EqualTo(30));
    }

    [Test]
    public async Task WriteCsv_LeavesTheStreamOpenAndRewound()
    {
        var stream = new MemoryStream();

        await new ExcelConverter<PersonClass>().WriteCsvAsync(stream, people);

        Assert.That(stream.CanRead, Is.True);
        Assert.That(stream.Position, Is.Zero);
        Assert.That(await new StreamReader(stream).ReadToEndAsync(), Is.EqualTo("Name,Age,Company\r\nAlice,30,Tech\r\n"));
    }

    [Test]
    public async Task ReadCsv_LeavesTheStreamOpen()
    {
        var stream = new MemoryStream("Name,Age,Company\r\nBob,25,Corp\r\n"u8.ToArray());

        await new ExcelConverter<PersonClass>().ReadCsvAsync(stream);

        Assert.That(stream.CanRead, Is.True);
    }

    [Test]
    public void WriteExcel_NullArguments_Throw()
    {
        var converter = new ExcelConverter<PersonClass>();

        Assert.Throws<ArgumentNullException>(() => converter.WriteExcelAsync(null!, people));
        Assert.Throws<ArgumentNullException>(() => converter.WriteExcelAsync(new MemoryStream(), null!));
    }
}

public class ExcelWriterExcludeTests
{
    private static readonly List<PersonClass> people = [new() { Name = "Alice", Age = 30, Company = "Tech" }];

    [Test]
    public async Task Exclude_LeavesTheColumnOutOfExcel()
    {
        var converter = new ExcelConverter<PersonClass>();
        converter.AddMapping("Years", p => p.Age);
        converter.Exclude(p => p.Age);
        var stream = new MemoryStream();

        await converter.WriteExcelAsync(stream, people);

        using var workbook = new XSSFWorkbook(stream);
        var header = workbook.GetSheetAt(0).GetRow(0);
        Assert.That(header.Cells.Select(c => c.StringCellValue), Is.EqualTo(new[] { "Name", "Company" }));
        Assert.That(workbook.GetSheetAt(0).GetRow(1).GetCell(1).StringCellValue, Is.EqualTo("Tech"));
    }

    [Test]
    public async Task Exclude_LeavesTheColumnOutOfCsv()
    {
        var converter = new ExcelConverter<PersonClass>();
        converter.Exclude(p => p.Company);
        var stream = new MemoryStream();

        await converter.WriteCsvAsync(stream, people);

        Assert.That(await new StreamReader(stream).ReadToEndAsync(), Is.EqualTo("Name,Age\r\nAlice,30\r\n"));
    }

    [Test]
    public async Task Exclude_IgnoresTheColumnWhenReadingCsv()
    {
        var converter = new ExcelConverter<PersonClass>();
        converter.Exclude(p => p.Company);
        var stream = new MemoryStream("Name,Age,Company\r\nBob,25,Corp\r\n"u8.ToArray());

        var person = (await converter.ReadCsvAsync(stream)).Single();

        Assert.That(person.Name, Is.EqualTo("Bob"));
        Assert.That(person.Company, Is.Null);
    }
}

public class ExcelWriterValueTests
{
    private static readonly DateTime morning = new(2026, 11, 24, 9, 30, 0);

    private static ISheet Write<T>(ExcelConverter<T> converter, IEnumerable<T> records)
    {
        var stream = new MemoryStream();
        converter.WriteExcelAsync(stream, records).Wait();
        return new XSSFWorkbook(stream).GetSheetAt(0);
    }

    [Test]
    public void Dates_AreWrittenAsFormattedDateCells()
    {
        var sheet = Write(new ExcelConverter<DatedRow>(),
            [new() { Name = "A", When = morning, Day = new DateOnly(2026, 11, 24), Stamp = new DateTimeOffset(morning, TimeSpan.FromHours(-5)) }]);
        var row = sheet.GetRow(1);

        foreach (var (index, format) in new[] { (1, "yyyy-mm-dd hh:mm:ss"), (2, "yyyy-mm-dd"), (4, "yyyy-mm-dd hh:mm:ss") })
        {
            var cell = row.GetCell(index);
            Assert.That(cell.CellType, Is.EqualTo(CellType.Numeric));
            Assert.That(cell.CellStyle.GetDataFormatString(), Is.EqualTo(format));
            Assert.That(DateUtil.IsCellDateFormatted(cell), Is.True);
        }

        Assert.That(row.GetCell(1).DateCellValue, Is.EqualTo(morning));
        Assert.That(row.GetCell(2).DateCellValue, Is.EqualTo(new DateTime(2026, 11, 24)));
        Assert.That(row.GetCell(3).CellType, Is.EqualTo(CellType.Blank));
        Assert.That(row.GetCell(4).DateCellValue, Is.EqualTo(morning));
    }

    [Test]
    public void Dates_UseTheConverterFormats()
    {
        var converter = new ExcelConverter<DatedRow> { DateTimeFormat = "m/d/yy h:mm", DateFormat = "d-mmm-yy" };

        var row = Write(converter, [new() { Name = "A", When = morning, Day = new DateOnly(2026, 11, 24) }]).GetRow(1);

        Assert.That(row.GetCell(1).CellStyle.GetDataFormatString(), Is.EqualTo("m/d/yy h:mm"));
        Assert.That(row.GetCell(2).CellStyle.GetDataFormatString(), Is.EqualTo("d-mmm-yy"));
    }

    [Test]
    public void Dates_RoundTrip()
    {
        var converter = new ExcelConverter<DatedRow>();
        List<DatedRow> rows =
        [
            new() { Name = "A", When = morning, Day = new DateOnly(2026, 11, 24), OptionalDay = new DateOnly(2027, 1, 2), Stamp = morning },
            new() { Name = "B", When = morning.AddDays(1), Day = new DateOnly(2026, 11, 25), OptionalDay = null, Stamp = morning }
        ];
        var stream = new MemoryStream();
        converter.WriteExcelAsync(stream, rows).Wait();

        var back = converter.ReadExcel(stream);

        Assert.That(back[0].When, Is.EqualTo(morning));
        Assert.That(back[0].Day, Is.EqualTo(new DateOnly(2026, 11, 24)));
        Assert.That(back[0].OptionalDay, Is.EqualTo(new DateOnly(2027, 1, 2)));
        Assert.That(back[1].OptionalDay, Is.Null);
        Assert.That(back[1].When, Is.EqualTo(morning.AddDays(1)));
    }

    [Test]
    public void UnsignedAndSmallIntegers_AreWrittenAsNumbers()
    {
        var row = Write(new ExcelConverter<UnsignedRow>(), [new() { A = 4000000000, B = 5, C = 65535, D = -8 }]).GetRow(1);

        Assert.That(row.Cells.Select(c => c.CellType), Is.All.EqualTo(CellType.Numeric));
        Assert.That(row.Cells.Select(c => c.NumericCellValue), Is.EqualTo(new double[] { 4000000000, 5, 65535, -8 }));
    }

    [Test]
    public void StaticProperties_AreNotWritten()
    {
        var header = Write(new ExcelConverter<StaticPropertyRow>(), [new() { Name = "A" }]).GetRow(0);

        Assert.That(header.Cells.Select(c => c.StringCellValue), Is.EqualTo(new[] { "Name" }));
    }

    [Test]
    public void Records_AreEnumeratedOnce()
    {
        var enumerations = 0;
        IEnumerable<PersonClass> Records()
        {
            enumerations++;
            yield return new PersonClass { Name = "Alice", Age = 30, Company = "Tech" };
        }

        var sheet = Write(new ExcelConverter<PersonClass>(), Records());

        Assert.That(enumerations, Is.EqualTo(1));
        Assert.That(sheet.GetRow(1).GetCell(0).StringCellValue, Is.EqualTo("Alice"));
    }

    [Test]
    public void ColumnWidth_NeverExceedsExcelsLimit()
    {
        var converter = new ExcelConverter<PersonClass> { MaxCellWidth = 0 };

        var sheet = Write(converter, [new() { Name = new string('x', 1000), Age = 1, Company = "C" }]);

        Assert.That(sheet.GetColumnWidth(0), Is.EqualTo(255 * 256));
    }
}

public class ExcelWorkbookWriterTests
{
    private static readonly DateTime morning = new(2026, 11, 24, 9, 30, 0);

    [Test]
    public void Sheets_OfDifferentTypes_GoIntoOneWorkbook()
    {
        var people = new ExcelConverter<PersonClass>();
        people.AddMapping("Customer Name", p => p.Name);
        var searches = new ExcelConverter<SearchRow>();
        searches.AddMapping("Customer Name", s => s.CustomerName);

        using var writer = new ExcelWorkbookWriter();
        writer.AddSheet("People", [new PersonClass { Name = "Alice", Age = 30, Company = "Tech" }], people)
              .AddSheet("Searches", [new SearchRow { Search = "F-150", CustomerName = "Bob", Date = morning }], searches)
              .AddSheet("Plain", [new PersonClass { Name = "Carol", Age = 50, Company = "Corp" }]);
        var stream = new MemoryStream();
        writer.Write(stream);

        Assert.That(writer.SheetCount, Is.EqualTo(3));
        Assert.That(stream.Position, Is.Zero);

        using (var workbook = new XSSFWorkbook(new MemoryStream(stream.ToArray())))
        {
            Assert.That(Enumerable.Range(0, workbook.NumberOfSheets).Select(workbook.GetSheetName),
                Is.EqualTo(new[] { "People", "Searches", "Plain" }));
            Assert.That(workbook.GetSheet("People").GetRow(0).GetCell(0).StringCellValue, Is.EqualTo("Customer Name"));
            Assert.That(workbook.GetSheet("Searches").GetRow(0).GetCell(1).StringCellValue, Is.EqualTo("Customer Name"));
            Assert.That(workbook.GetSheet("Plain").GetRow(0).GetCell(0).StringCellValue, Is.EqualTo("Name"));
        }

        Assert.That(people.ReadExcel(new MemoryStream(stream.ToArray()), "People").Single().Name, Is.EqualTo("Alice"));
        var search = searches.ReadExcel(new MemoryStream(stream.ToArray()), "Searches").Single();
        Assert.That(search.CustomerName, Is.EqualTo("Bob"));
        Assert.That(search.Date, Is.EqualTo(morning));
        Assert.That(new ExcelConverter<PersonClass>().ReadExcel(new MemoryStream(stream.ToArray()), "Plain").Single().Age, Is.EqualTo(50));
    }

    [Test]
    public void Sheet_WithNoRecords_HasTheHeaderOnly()
    {
        using var writer = new ExcelWorkbookWriter();
        writer.AddSheet("Empty", new List<PersonClass>());
        var stream = new MemoryStream();
        writer.Write(stream);

        using var workbook = new XSSFWorkbook(stream);
        var sheet = workbook.GetSheet("Empty");
        Assert.That(sheet.LastRowNum, Is.Zero);
        Assert.That(sheet.GetRow(0).Cells.Select(c => c.StringCellValue), Is.EqualTo(new[] { "Name", "Age", "Company" }));
    }

    [Test]
    public void Styles_AreSharedAcrossSheets()
    {
        int StylesFor(int sheets)
        {
            using var writer = new ExcelWorkbookWriter();
            for (var i = 0; i < sheets; i++)
            {
                writer.AddSheet($"S{i}", [new DatedRow { Name = "A", When = morning, Day = new DateOnly(2026, 11, 24) }]);
            }

            var stream = new MemoryStream();
            writer.Write(stream);
            using var workbook = new XSSFWorkbook(stream);
            return workbook.NumCellStyles;
        }

        Assert.That(StylesFor(10), Is.EqualTo(StylesFor(1)));
    }

    [Test]
    public void InvalidUse_Throws()
    {
        using var writer = new ExcelWorkbookWriter();

        Assert.Throws<InvalidOperationException>(() => writer.Write(new MemoryStream()));
        Assert.Throws<ArgumentException>(() => writer.AddSheet(" ", new List<PersonClass>()));
        Assert.Throws<ArgumentNullException>(() => writer.AddSheet<PersonClass>("S", null!));

        writer.AddSheet("Report", new List<PersonClass>());
        Assert.Throws<ArgumentException>(() => writer.AddSheet("report", new List<PersonClass>()));
        Assert.Throws<ArgumentNullException>(() => writer.Write(null!));
    }
}

public class ExcelContentTypeTests
{
    [Test]
    public void ContentTypes_MatchTheFormats()
    {
        Assert.That(ExcelContentType.Xlsx, Is.EqualTo("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"));
        Assert.That(ExcelContentType.Xls, Is.EqualTo("application/vnd.ms-excel"));
        Assert.That(ExcelContentType.Csv, Is.EqualTo("text/csv"));
    }
}
