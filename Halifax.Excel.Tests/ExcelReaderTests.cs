using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace Halifax.Excel.Tests;

#pragma warning disable CS8618

class ValueRow<T>
{
    public string Name { get; set; }
    public T Value { get; set; }
}

class StatusRow
{
    public string Name { get; set; }
    public string Status { get; set; } = "New";
    public int Count { get; set; } = 5;
}

record CouponRow(
    string Title,
    string Description,
    string? Disclaimer = null,
    DateOnly? StartDate = null);

/// <summary>Builds small workbooks cell by cell with NPOI, in either file format.</summary>
static class Sheets
{
    public static byte[] Build(bool xls, Action<IWorkbook, ISheet> fill, string sheetName = "Sheet1")
    {
        IWorkbook workbook = xls ? new HSSFWorkbook() : new XSSFWorkbook();
        fill(workbook, workbook.CreateSheet(sheetName));
        workbook.GetCreationHelper().CreateFormulaEvaluator().EvaluateAll();

        using var stream = new MemoryStream();
        workbook.Write(stream, true);
        return stream.ToArray();
    }

    public static ICellStyle Format(IWorkbook workbook, string format)
    {
        var style = workbook.CreateCellStyle();
        style.DataFormat = workbook.CreateDataFormat().GetFormat(format);
        return style;
    }

    /// <summary>Writes a row: strings as text, numbers as numbers, DateTime as a date-formatted number, null as no cell.</summary>
    public static void Row(IWorkbook workbook, ISheet sheet, int index, params object?[] values)
    {
        var row = sheet.CreateRow(index);
        for (var i = 0; i < values.Length; i++)
        {
            switch (values[i])
            {
                case null: break;
                case string text: row.CreateCell(i).SetCellValue(text); break;
                case bool flag: row.CreateCell(i).SetCellValue(flag); break;
                case DateTime date:
                    var cell = row.CreateCell(i);
                    cell.SetCellValue(date);
                    cell.CellStyle = Format(workbook, "m/d/yy h:mm");
                    break;
                default: row.CreateCell(i).SetCellValue(Convert.ToDouble(values[i])); break;
            }
        }
    }

    /// <summary>Reads the single data row of a "Name | Value" sheet whose Value cell is set by <paramref name="set"/>.</summary>
    public static T ReadValue<T>(bool xls, Action<IWorkbook, ICell> set)
    {
        var bytes = Build(xls, (workbook, sheet) =>
        {
            Row(workbook, sheet, 0, "Name", "Value");
            var row = sheet.CreateRow(1);
            row.CreateCell(0).SetCellValue("row");
            set(workbook, row.CreateCell(1));
        });

        var records = new ExcelConverter<ValueRow<T>>().ReadExcel(new MemoryStream(bytes));
        Assert.That(records, Has.Count.EqualTo(1));
        return records[0].Value;
    }
}

public class ExcelReaderNumberTests
{
    [Test]
    public void Number_IntoString_ReadsPlainText([Values] bool xls)
    {
        Assert.That(Sheets.ReadValue<string>(xls, (_, c) => c.SetCellValue(1234)), Is.EqualTo("1234"));
        Assert.That(Sheets.ReadValue<string>(xls, (_, c) => c.SetCellValue(4165551234)), Is.EqualTo("4165551234"));
        Assert.That(Sheets.ReadValue<string>(xls, (_, c) => c.SetCellValue(12.5)), Is.EqualTo("12.5"));
        Assert.That(Sheets.ReadValue<string>(xls, (_, c) => c.SetCellValue(0.1 + 0.2)), Is.EqualTo("0.3"));
    }

    [TestCase(false, "000000", 1234, "001234")]
    [TestCase(true, "000000", 1234, "001234")]
    [TestCase(false, "#,##0.00", 1234.5, "1,234.50")]
    [TestCase(true, "#,##0.00", 1234.5, "1,234.50")]
    [TestCase(false, "0%", 0.5, "50%")]
    [TestCase(true, "0%", 0.5, "50%")]
    public void Number_WithDigitFormat_IntoString_KeepsTheFormat(bool xls, string format, double value, string expected)
    {
        var text = Sheets.ReadValue<string>(xls, (w, c) =>
        {
            c.SetCellValue(value);
            c.CellStyle = Sheets.Format(w, format);
        });

        Assert.That(text, Is.EqualTo(expected));
    }

    [Test]
    public void Number_WithCurrencyFormat_IntoString_FallsBackToThePlainNumber([Values] bool xls)
    {
        var text = Sheets.ReadValue<string>(xls, (w, c) =>
        {
            c.SetCellValue(1234.5);
            c.CellStyle = Sheets.Format(w, "\"$\"#,##0.00");
        });

        Assert.That(text, Is.EqualTo("1234.5"));
    }

    [Test]
    public void Number_IntoNumericTypes([Values] bool xls)
    {
        Assert.That(Sheets.ReadValue<int>(xls, (_, c) => c.SetCellValue(42)), Is.EqualTo(42));
        Assert.That(Sheets.ReadValue<long>(xls, (_, c) => c.SetCellValue(9999999999)), Is.EqualTo(9999999999L));
        Assert.That(Sheets.ReadValue<decimal>(xls, (_, c) => c.SetCellValue(123.45)), Is.EqualTo(123.45m));
        Assert.That(Sheets.ReadValue<decimal?>(xls, (_, c) => c.SetCellValue(7)), Is.EqualTo(7m));
        Assert.That(Sheets.ReadValue<decimal>(xls, (_, c) => c.SetCellValue(19.99)), Is.EqualTo(19.99m));
        Assert.That(Sheets.ReadValue<decimal>(xls, (_, c) => c.SetCellValue(0.1 + 0.2)), Is.EqualTo(0.3m));
        Assert.That(Sheets.ReadValue<decimal>(xls, (_, c) => c.SetCellValue(1234567.89)), Is.EqualTo(1234567.89m));
        Assert.Throws<ExcelConversionException>(() => Sheets.ReadValue<decimal>(xls, (_, c) => c.SetCellValue(1e30)));
        Assert.That(Sheets.ReadValue<double>(xls, (_, c) => c.SetCellValue(3.25)), Is.EqualTo(3.25));
    }

    [Test]
    public void Number_IntoBoolAndEnum([Values] bool xls)
    {
        Assert.That(Sheets.ReadValue<bool>(xls, (_, c) => c.SetCellValue(1)), Is.True);
        Assert.That(Sheets.ReadValue<bool>(xls, (_, c) => c.SetCellValue(0)), Is.False);
        Assert.That(Sheets.ReadValue<Status>(xls, (_, c) => c.SetCellValue(2)), Is.EqualTo(Status.Pending));
    }

    [Test]
    public void Number_TooLargeForTheProperty_ThrowsWithRowAndColumn([Values] bool xls)
    {
        var e = Assert.Throws<ExcelConversionException>(() => Sheets.ReadValue<byte>(xls, (_, c) => c.SetCellValue(300)));

        Assert.That(e!.Row, Is.EqualTo(2));
        Assert.That(e.Column, Is.EqualTo("Value"));
        Assert.That(e.InnerException, Is.InstanceOf<OverflowException>());
    }
}

public class ExcelReaderDateTests
{
    private static readonly DateTime morning = new(2026, 11, 24, 9, 30, 0);

    private static void SetDate(IWorkbook workbook, ICell cell, DateTime value, string format = "m/d/yy h:mm")
    {
        cell.SetCellValue(value);
        cell.CellStyle = Sheets.Format(workbook, format);
    }

    [Test]
    public void DateCell_IntoDateTypes([Values] bool xls)
    {
        Assert.That(Sheets.ReadValue<DateTime>(xls, (w, c) => SetDate(w, c, morning)), Is.EqualTo(morning));
        Assert.That(Sheets.ReadValue<DateTime?>(xls, (w, c) => SetDate(w, c, morning)), Is.EqualTo(morning));
        Assert.That(Sheets.ReadValue<DateOnly>(xls, (w, c) => SetDate(w, c, morning)), Is.EqualTo(new DateOnly(2026, 11, 24)));
        Assert.That(Sheets.ReadValue<DateOnly?>(xls, (w, c) => SetDate(w, c, morning)), Is.EqualTo(new DateOnly(2026, 11, 24)));
        Assert.That(Sheets.ReadValue<TimeOnly>(xls, (w, c) => SetDate(w, c, morning)), Is.EqualTo(new TimeOnly(9, 30)));
        Assert.That(Sheets.ReadValue<DateTimeOffset>(xls, (w, c) => SetDate(w, c, morning)).DateTime, Is.EqualTo(morning));
    }

    [Test]
    public void DateCell_IntoString_ReadsIsoText([Values] bool xls)
    {
        Assert.That(Sheets.ReadValue<string>(xls, (w, c) => SetDate(w, c, morning)), Is.EqualTo("2026-11-24 09:30:00"));
        Assert.That(Sheets.ReadValue<string>(xls, (w, c) => SetDate(w, c, morning.Date, "yyyy-mm-dd")), Is.EqualTo("2026-11-24"));
    }

    [Test]
    public void UnformattedNumber_IntoDateTime_ReadsAsExcelSerialDate([Values] bool xls)
    {
        // 46350 is 2026-11-24 in Excel's 1900 date system
        Assert.That(Sheets.ReadValue<DateOnly>(xls, (_, c) => c.SetCellValue(46350)), Is.EqualTo(new DateOnly(2026, 11, 24)));
    }
}

public class ExcelReaderTextTests
{
    [Test]
    public void Text_IsParsedIntoThePropertyType([Values] bool xls)
    {
        Assert.That(Sheets.ReadValue<int>(xls, (_, c) => c.SetCellValue(" 42 ")), Is.EqualTo(42));
        Assert.That(Sheets.ReadValue<decimal>(xls, (_, c) => c.SetCellValue("1,234.50")), Is.EqualTo(1234.50m));
        Assert.That(Sheets.ReadValue<double?>(xls, (_, c) => c.SetCellValue("1E+3")), Is.EqualTo(1000d));
        Assert.That(Sheets.ReadValue<Status>(xls, (_, c) => c.SetCellValue("pending")), Is.EqualTo(Status.Pending));
        Assert.That(Sheets.ReadValue<DateOnly>(xls, (_, c) => c.SetCellValue("2026-11-24")), Is.EqualTo(new DateOnly(2026, 11, 24)));
        Assert.That(Sheets.ReadValue<DateTime>(xls, (_, c) => c.SetCellValue("11/24/2026 09:30")), Is.EqualTo(new DateTime(2026, 11, 24, 9, 30, 0)));
        Assert.That(Sheets.ReadValue<TimeSpan>(xls, (_, c) => c.SetCellValue("01:30:00")), Is.EqualTo(TimeSpan.FromMinutes(90)));

        var id = Guid.NewGuid();
        Assert.That(Sheets.ReadValue<Guid>(xls, (_, c) => c.SetCellValue(id.ToString())), Is.EqualTo(id));
    }

    [TestCase("TRUE", true)]
    [TestCase("false", false)]
    [TestCase("yes", true)]
    [TestCase("N", false)]
    [TestCase("1", true)]
    public void Text_IntoBool(string text, bool expected)
    {
        Assert.That(Sheets.ReadValue<bool>(false, (_, c) => c.SetCellValue(text)), Is.EqualTo(expected));
    }

    [Test]
    public void Text_IntoString_IsKeptAsWritten([Values] bool xls)
    {
        Assert.That(Sheets.ReadValue<string>(xls, (_, c) => c.SetCellValue("  spaced  ")), Is.EqualTo("  spaced  "));
    }

    [Test]
    public void Text_UsesTheConverterCulture()
    {
        var bytes = Sheets.Build(false, (w, s) =>
        {
            Sheets.Row(w, s, 0, "Name", "Value");
            Sheets.Row(w, s, 1, "row", "1.234,5");
        });
        var converter = new ExcelConverter<ValueRow<decimal>> { CultureInfo = new System.Globalization.CultureInfo("de-DE") };

        Assert.That(converter.ReadExcel(new MemoryStream(bytes))[0].Value, Is.EqualTo(1234.5m));
    }

    [Test]
    public void Text_ThatDoesNotParse_ThrowsWithRowColumnAndValue([Values] bool xls)
    {
        var bytes = Sheets.Build(xls, (w, s) =>
        {
            Sheets.Row(w, s, 0, "Full Name", "Age", "Company");
            Sheets.Row(w, s, 1, "Alice", 30, "Tech");
            Sheets.Row(w, s, 2, "Bob", "abc", "Corp");
        });
        var converter = new ExcelConverter<PersonClass>();
        converter.AddMapping("Full Name", p => p.Name);

        var e = Assert.Throws<ExcelConversionException>(() => converter.ReadExcel(new MemoryStream(bytes)));

        Assert.That(e!.Row, Is.EqualTo(3));
        Assert.That(e.Column, Is.EqualTo("Age"));
        Assert.That(e.Value, Is.EqualTo("abc"));
        Assert.That(e.TargetType, Is.EqualTo(typeof(int)));
        Assert.That(e.Message, Is.EqualTo("Cannot read 'abc' in row 3, column 'Age' as Int32"));
        Assert.That(e, Is.InstanceOf<FormatException>());
    }
}

public class ExcelReaderCellKindTests
{
    [Test]
    public void BooleanCell_IntoBoolAndString([Values] bool xls)
    {
        Assert.That(Sheets.ReadValue<bool>(xls, (_, c) => c.SetCellValue(true)), Is.True);
        Assert.That(Sheets.ReadValue<string>(xls, (_, c) => c.SetCellValue(false)), Is.EqualTo("FALSE"));
    }

    [Test]
    public void FormulaCell_ReadsItsCalculatedResult([Values] bool xls)
    {
        var bytes = Sheets.Build(xls, (w, s) =>
        {
            Sheets.Row(w, s, 0, "Name", "Value", "Text");
            Sheets.Row(w, s, 1, "row", 1234);
            s.GetRow(1).CreateCell(1).SetCellFormula("617*2");
            s.GetRow(1).CreateCell(2).SetCellFormula("\"A\"&\"B\"");
        });

        var record = new ExcelConverter<FormulaRow>().ReadExcel(new MemoryStream(bytes)).Single();

        Assert.That(record.Value, Is.EqualTo(1234));
        Assert.That(record.Text, Is.EqualTo("AB"));
    }

    [Test]
    public void NumericFormulaCell_IntoString_ReadsTheResultNotTheFormula([Values] bool xls)
    {
        Assert.That(Sheets.ReadValue<string>(xls, (_, c) => c.SetCellFormula("617*2")), Is.EqualTo("1234"));
    }

    [Test]
    public void ErrorCell_IsReadAsBlank([Values] bool xls)
    {
        Assert.That(Sheets.ReadValue<int?>(xls, (_, c) => c.SetCellErrorValue(FormulaError.DIV0.Code)), Is.Null);
    }

    private class FormulaRow
    {
        public string Name { get; set; }
        public int Value { get; set; }
        public string Text { get; set; }
    }
}

public class ExcelReaderLayoutTests
{
    [Test]
    public void Headers_MatchIgnoringCaseAndSpaces_InAnyOrder([Values] bool xls)
    {
        var bytes = Sheets.Build(xls, (w, s) =>
        {
            Sheets.Row(w, s, 0, "COMPANY", "  full NAME ", "age", "Not A Property");
            Sheets.Row(w, s, 1, "Tech", "Alice", 30, "ignored");
        });
        var converter = new ExcelConverter<PersonClass>();
        converter.AddMapping("Full Name", p => p.Name);

        var person = converter.ReadExcel(new MemoryStream(bytes)).Single();

        Assert.That(person.Name, Is.EqualTo("Alice"));
        Assert.That(person.Age, Is.EqualTo(30));
        Assert.That(person.Company, Is.EqualTo("Tech"));
    }

    [Test]
    public void BlankRows_AreSkipped([Values] bool xls)
    {
        var bytes = Sheets.Build(xls, (w, s) =>
        {
            Sheets.Row(w, s, 0, "Name", "Age", "Company");
            Sheets.Row(w, s, 1, "Alice", 30, "Tech");
            Sheets.Row(w, s, 3, "  ", null, "");
            Sheets.Row(w, s, 4, "Bob", 40, "Corp");
            Sheets.Row(w, s, 6);
        });

        var people = new ExcelConverter<PersonClass>().ReadExcel(new MemoryStream(bytes));

        Assert.That(people.Select(p => p.Name), Is.EqualTo(new[] { "Alice", "Bob" }));
    }

    [Test]
    public void BlankCell_LeavesThePropertyAtItsDefault([Values] bool xls)
    {
        var bytes = Sheets.Build(xls, (w, s) =>
        {
            Sheets.Row(w, s, 0, "Name", "Status", "Count");
            Sheets.Row(w, s, 1, "Alice", null, " ");
            Sheets.Row(w, s, 2, "Bob", "Done", 2);
        });

        var rows = new ExcelConverter<StatusRow>().ReadExcel(new MemoryStream(bytes));

        Assert.That(rows[0].Status, Is.EqualTo("New"));
        Assert.That(rows[0].Count, Is.EqualTo(5));
        Assert.That(rows[1].Status, Is.EqualTo("Done"));
        Assert.That(rows[1].Count, Is.EqualTo(2));
    }

    [Test]
    public void ConstructorRecord_WithBlankCellsAndOptionalDate([Values] bool xls)
    {
        var bytes = Sheets.Build(xls, (w, s) =>
        {
            Sheets.Row(w, s, 0, "Title", "Description", "Disclaimer", "Start Date");
            Sheets.Row(w, s, 1, "Oil change", "Ten percent off", "Some limits", new DateTime(2026, 12, 1));
            Sheets.Row(w, s, 2, "Tires", null, null, null);
        });
        var converter = new ExcelConverter<CouponRow>();
        converter.AddMapping("Start Date", c => c.StartDate!);

        var coupons = converter.ReadExcel(new MemoryStream(bytes));

        Assert.That(coupons[0], Is.EqualTo(new CouponRow("Oil change", "Ten percent off", "Some limits", new DateOnly(2026, 12, 1))));
        Assert.That(coupons[1], Is.EqualTo(new CouponRow("Tires", null!)));
    }

    [Test]
    public void ExcludedProperty_IsNotRead([Values] bool xls)
    {
        var bytes = Sheets.Build(xls, (w, s) =>
        {
            Sheets.Row(w, s, 0, "Name", "Age", "Company");
            Sheets.Row(w, s, 1, "Alice", 30, "Tech");
        });
        var converter = new ExcelConverter<PersonClass>();
        converter.Exclude(p => p.Company);

        var person = converter.ReadExcel(new MemoryStream(bytes)).Single();

        Assert.That(person.Name, Is.EqualTo("Alice"));
        Assert.That(person.Company, Is.Null);
    }

    [Test]
    public void HeaderRow_BelowEmptyRows_IsFound([Values] bool xls)
    {
        var bytes = Sheets.Build(xls, (w, s) =>
        {
            Sheets.Row(w, s, 2, "Name", "Age", "Company");
            Sheets.Row(w, s, 3, "Alice", 30, "Tech");
        });

        Assert.That(new ExcelConverter<PersonClass>().ReadExcel(new MemoryStream(bytes)).Single().Name, Is.EqualTo("Alice"));
    }

    [Test]
    public void EmptySheet_ReadsNothing([Values] bool xls)
    {
        var bytes = Sheets.Build(xls, (_, _) => { });

        Assert.That(new ExcelConverter<PersonClass>().ReadExcel(new MemoryStream(bytes)), Is.Empty);
    }

    [Test]
    public void NamedSheet_IsRead_CaseInsensitively([Values] bool xls)
    {
        IWorkbook workbook = xls ? new HSSFWorkbook() : new XSSFWorkbook();
        Sheets.Row(workbook, workbook.CreateSheet("First"), 0, "Name");
        var second = workbook.CreateSheet("People");
        Sheets.Row(workbook, second, 0, "Name", "Age");
        Sheets.Row(workbook, second, 1, "Alice", 30);
        using var stream = new MemoryStream();
        workbook.Write(stream, true);
        var bytes = stream.ToArray();
        var converter = new ExcelConverter<PersonClass>();

        Assert.That(converter.ReadExcel(new MemoryStream(bytes), "people").Single().Age, Is.EqualTo(30));
        Assert.That(converter.ReadExcel(new MemoryStream(bytes)), Is.Empty);
        Assert.Throws<ArgumentException>(() => converter.ReadExcel(new MemoryStream(bytes), "Missing"));
        Assert.Throws<ArgumentException>(() => converter.ReadExcel(new MemoryStream(bytes), " "));
    }

    [Test]
    public async Task ReadAsync_LegacyXlsFile_ReadsNumbersIntoText()
    {
        var bytes = Sheets.Build(true, (w, s) =>
        {
            Sheets.Row(w, s, 0, "Number", "Last Name", "Job Title");
            Sheets.Row(w, s, 1, 1234, "Smith", "Tech");
        });
        var converter = new ExcelConverter<EmployeeRow>();
        converter.AddMapping("Last Name", e => e.Name);
        converter.AddMapping("Job Title", e => e.Title);

        var employee = (await converter.ReadAsync(new MemoryStream(bytes), ExcelContentType.Xls)).Single();

        Assert.That(employee.Number, Is.EqualTo("1234"));
        Assert.That(employee.Name, Is.EqualTo("Smith"));
        Assert.That(employee.Title, Is.EqualTo("Tech"));
    }

    private class EmployeeRow
    {
        public string Number { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
    }
}

public class ExcelCellTests
{
    [Test]
    public void GetText_CoversEveryCellKind([Values] bool xls)
    {
        IWorkbook workbook = xls ? new HSSFWorkbook() : new XSSFWorkbook();
        var row = workbook.CreateSheet("S").CreateRow(0);
        row.CreateCell(0).SetCellValue("text");
        row.CreateCell(1).SetCellValue(1234);
        row.CreateCell(2).SetCellValue(true);
        row.CreateCell(3).SetCellErrorValue(FormulaError.DIV0.Code);
        row.CreateCell(4);
        row.CreateCell(5).SetCellFormula("2*3");
        var date = row.CreateCell(6);
        date.SetCellValue(new DateTime(2026, 11, 24));
        date.CellStyle = Sheets.Format(workbook, "d-mmm-yy");
        workbook.GetCreationHelper().CreateFormulaEvaluator().EvaluateAll();

        Assert.That(ExcelCell.GetText(null), Is.Null);
        Assert.That(ExcelCell.GetText(row.GetCell(0)), Is.EqualTo("text"));
        Assert.That(ExcelCell.GetText(row.GetCell(1)), Is.EqualTo("1234"));
        Assert.That(ExcelCell.GetText(row.GetCell(2)), Is.EqualTo("TRUE"));
        Assert.That(ExcelCell.GetText(row.GetCell(3)), Is.EqualTo("#DIV/0!"));
        Assert.That(ExcelCell.GetText(row.GetCell(4)), Is.Empty);
        Assert.That(ExcelCell.GetText(row.GetCell(5)), Is.EqualTo("6"));
        Assert.That(ExcelCell.GetText(row.GetCell(6)), Is.EqualTo("2026-11-24"));
    }
}

/// <summary>
/// Halifax.Excel promises to read and write without SkiaSharp. These tests run without it on disk; the guard
/// below fails if it ever ships into the output or gets loaded, which would hide a reader that needs it again.
/// </summary>
public class ExcelSkiaSharpGuardTests
{
    [Test]
    public void SkiaSharp_IsNeitherShippedNorLoaded()
    {
        foreach (var xls in new[] { false, true })
        {
            Assert.That(Sheets.ReadValue<string>(xls, (_, c) => c.SetCellValue(1234)), Is.EqualTo("1234"));
        }

        var written = new MemoryStream();
        new ExcelConverter<PersonClass>().WriteExcelAsync(written, [new() { Name = "A", Age = 1, Company = "B" }]).Wait();

        Assert.That(File.Exists(Path.Combine(AppContext.BaseDirectory, "SkiaSharp.dll")), Is.False);
        Assert.That(AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetName().Name), Has.None.EqualTo("SkiaSharp"));
    }
}
