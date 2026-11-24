using System.Dynamic;
using System.Globalization;
using System.Text;

namespace Halifax.Excel.Tests;

/// <summary>
/// CsvFile replaces <c>new CsvWriter(writer, CultureInfo.InvariantCulture).WriteRecordsAsync(dynamicRows)</c>. The
/// expected text below is what CsvHelper 33.1.0 wrote for exactly these rows, byte for byte.
/// </summary>
public class CsvFileTests
{
    private static IDictionary<string, object?> Row(params (string Key, object? Value)[] cells)
    {
        IDictionary<string, object?> row = new ExpandoObject();
        foreach (var (key, value) in cells) row[key] = value;
        return row;
    }

    private static readonly List<IDictionary<string, object?>> rows =
    [
        Row(("Id", 42), ("Big", 9000000000L), ("Small", (short)-3), ("Tiny", (byte)7), ("Flag", true), ("Price", 1234.50m),
            ("Ratio", 0.1 + 0.2), ("Real", 2.5f), ("Name", "Smith, John"), ("Quote", "He said \"hi\""), ("Lines", "a\r\nb"),
            ("Lead", " lead"), ("Trail", "trail "), ("Tab", "\ttab"), ("Empty", ""), ("Null", null),
            ("When", new DateTime(2026, 11, 24, 9, 30, 5)), ("Day", new DateTime(2026, 11, 24)),
            ("Offset", new DateTimeOffset(2026, 11, 24, 9, 30, 0, TimeSpan.FromHours(-5))), ("Time", new TimeSpan(1, 2, 3)),
            ("Guid", Guid.Parse("a1b2c3d4-0000-1111-2222-333344445555")), ("Bytes", new byte[] { 0x0A, 0xFF, 0x00 }), ("Char", 'x'),
            ("Formula", "=SUM(A1)"), ("Semi", "a;b"), ("Cr", "x\ry"), ("Lf", "x\ny")),
        Row(("Id", 0), ("Big", 0L), ("Small", (short)0), ("Tiny", (byte)0), ("Flag", false), ("Price", 0m),
            ("Ratio", 1e20), ("Real", -0.5f), ("Name", "plain"), ("Quote", "\""), ("Lines", ""),
            ("Lead", "x"), ("Trail", "y"), ("Tab", "tab\t"), ("Empty", " "), ("Null", null),
            ("When", DateTime.MinValue), ("Day", new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc)),
            ("Offset", DateTimeOffset.MinValue), ("Time", TimeSpan.Zero),
            ("Guid", Guid.Empty), ("Bytes", Array.Empty<byte>()), ("Char", ','),
            ("Formula", "+1"), ("Semi", "x"), ("Cr", "\r"), ("Lf", "\n"))
    ];

    private const string csvHelper33Output =
        "Id,Big,Small,Tiny,Flag,Price,Ratio,Real,Name,Quote,Lines,Lead,Trail,Tab,Empty,Null,When,Day,Offset,Time,Guid,Bytes,Char,Formula,Semi,Cr,Lf\r\n" +
        "42,9000000000,-3,7,True,1234.50,0.30000000000000004,2.5,\"Smith, John\",\"He said \"\"hi\"\"\",\"a\r\nb\",\" lead\",\"trail \",\ttab,,," +
        "11/24/2026 09:30:05,11/24/2026 00:00:00,11/24/2026 09:30:00 -05:00,01:02:03,a1b2c3d4-0000-1111-2222-333344445555,0x0AFF00,x,=SUM(A1),a;b,\"x\ry\",\"x\ny\"\r\n" +
        "0,0,0,0,False,0,1E+20,-0.5,plain,\"\"\"\",,x,y,tab\t,\" \",," +
        "01/01/0001 00:00:00,01/02/2026 00:00:00,01/01/0001 00:00:00 +00:00,00:00:00,00000000-0000-0000-0000-000000000000,0x,\",\",+1,x,\"\r\",\"\n\"\r\n";

    private static async Task<byte[]> Write(IEnumerable<IDictionary<string, object?>> data, CultureInfo? culture = null)
    {
        var stream = new MemoryStream();
        await CsvFile.WriteAsync(stream, data, culture);
        return stream.ToArray();
    }

    [Test]
    public async Task DictionaryRows_MatchCsvHelper33ByteForByte()
    {
        var bytes = await Write(rows);

        Assert.That(Encoding.UTF8.GetString(bytes), Is.EqualTo(csvHelper33Output));
        Assert.That(bytes[0], Is.Not.EqualTo(0xEF), "no byte order mark");
    }

    [Test]
    public async Task DynamicRows_Work()
    {
        // e.g. Dapper's QueryAsync returns IEnumerable<dynamic> whose rows are dictionaries.
        List<dynamic> dynamicRows = rows.Cast<dynamic>().ToList();

        var bytes = await Write(dynamicRows.Cast<IDictionary<string, object?>>());

        Assert.That(Encoding.UTF8.GetString(bytes), Is.EqualTo(csvHelper33Output));
    }

    [Test]
    public async Task NoRows_WritesNothing()
    {
        Assert.That(await Write([]), Is.Empty);
    }

    [Test]
    public async Task Header_ComesFromTheFirstRow()
    {
        var bytes = await Write([Row(("A", 1), ("B", 2)), Row(("B", 3), ("C", 4))]);

        Assert.That(Encoding.UTF8.GetString(bytes), Is.EqualTo("A,B\r\n1,2\r\n,3\r\n"));
    }

    [Test]
    public async Task CultureSeparatorAndFormatting()
    {
        var bytes = await Write([Row(("Name", "a;b"), ("Price", 1.5m), ("Day", new DateTime(2026, 11, 24)))], new CultureInfo("de-DE"));

        Assert.That(Encoding.UTF8.GetString(bytes), Is.EqualTo("Name;Price;Day\r\n\"a;b\";1,5;24.11.2026 00:00:00\r\n"));
    }

    [Test]
    public async Task HeaderAndValues_Overload()
    {
        var stream = new MemoryStream();

        await CsvFile.WriteAsync(stream, ["Id", "Name"], [[1, "x, y"], [2, null]]);

        Assert.That(Encoding.UTF8.GetString(stream.ToArray()), Is.EqualTo("Id,Name\r\n1,\"x, y\"\r\n2,\r\n"));
    }

    [Test]
    public async Task Stream_IsLeftOpenAndRewound()
    {
        var stream = new MemoryStream();
        stream.Write([1, 2]);

        await CsvFile.WriteAsync(stream, [Row(("A", 1))]);

        Assert.That(stream.CanRead, Is.True);
        Assert.That(stream.Position, Is.EqualTo(2));
        Assert.That(Encoding.UTF8.GetString(stream.ToArray()[2..]), Is.EqualTo("A\r\n1\r\n"));
    }

    [Test]
    public async Task NullArguments_Throw()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => CsvFile.WriteAsync(null!, rows));
        await Assert.ThrowsAsync<ArgumentNullException>(() => CsvFile.WriteAsync(new MemoryStream(), (IEnumerable<IDictionary<string, object?>>)null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => CsvFile.WriteAsync(new MemoryStream(), null!, []));
    }
}
