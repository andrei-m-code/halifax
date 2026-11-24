using System.Globalization;
using System.Text;

namespace Halifax.Excel.Tests;

#pragma warning disable CS8618

class CsvRow
{
    public string A { get; set; }
    public string B { get; set; }
    public string C { get; set; }
}

/// <summary>
/// Halifax.Excel 6.1.0 read and wrote CSV through CsvHelper 34. These cases were run against 6.1.0 and record what it
/// returned; the built-in reader and writer must give exactly the same results.
/// </summary>
public class CsvCompatibilityTests
{
    private static async Task<string> ReadAll(string csv, CultureInfo? culture = null)
    {
        var converter = new ExcelConverter<CsvRow>();
        if (culture != null) converter.CultureInfo = culture;

        try
        {
            var rows = await converter.ReadCsvAsync(new MemoryStream(Encoding.UTF8.GetBytes(csv)));
            return string.Join(" | ", rows.Select(r => $"[{Show(r.A)},{Show(r.B)},{Show(r.C)}]"));
        }
        catch (Exception e)
        {
            return $"throws {e.GetType().Name}";
        }
    }

    private static string Show(string? s) => s == null ? "null" : $"<{s}>";

    [TestCase("A,B,C\r\nx,y,z\r\n", "[<x>,<y>,<z>]", TestName = "CRLF")]
    [TestCase("A,B,C\nx,y,z\n", "[<x>,<y>,<z>]", TestName = "LF")]
    [TestCase("A,B,C\rx,y,z\r", "[<x>,<y>,<z>]", TestName = "CR")]
    [TestCase("A,B,C\nx,y,z", "[<x>,<y>,<z>]", TestName = "No trailing newline")]
    [TestCase("A,B,C\n\nx,y,z\n\n", "[<x>,<y>,<z>]", TestName = "Blank lines skipped")]
    [TestCase("A,B,C\r\n\r\nx,y,z\r\n\r\n", "[<x>,<y>,<z>]", TestName = "Blank CRLF lines skipped")]
    [TestCase("\n\nA,B,C\nx,y,z\n", "[<x>,<y>,<z>]", TestName = "Blank lines before header")]
    [TestCase("A,B,C\n\rx,y,z\n", "[<x>,<y>,<z>]", TestName = "Lone CR is a blank line")]
    [TestCase("A,B,C\n   \nx,y,z\n", "throws IndexOutOfRangeException", TestName = "Whitespace line is a short record")]
    [TestCase("A,B,C\n\"\"\nx,y,z\n", "throws IndexOutOfRangeException", TestName = "Quoted empty line is a short record")]
    [TestCase("A,B,C\n\"a,1\",\"he said \"\"hi\"\"\",\"line1\nline2\"\n", "[<a,1>,<he said \"hi\">,<line1\nline2>]", TestName = "Quoted fields")]
    [TestCase("A,B,C\n\"l1\r\nl2\",y,z\r\n", "[<l1\r\nl2>,<y>,<z>]", TestName = "CRLF inside quotes")]
    [TestCase("A,B,C\r\"a\rb\",y,z\r", "[<a\rb>,<y>,<z>]", TestName = "CR inside quotes")]
    [TestCase("A,B,C\n\"\"\"\"\"\",y,z\n", "[<\"\">,<y>,<z>]", TestName = "Only quote characters")]
    [TestCase("A,B,C\n x , y ,z\n", "[< x >,< y >,<z>]", TestName = "Spaces kept")]
    [TestCase("A,B,C\nab\"c,d,e\n", "[<ab\"c>,<d>,<e>]", TestName = "Quote inside unquoted field")]
    [TestCase("A,B,C\n\"ab\"c,d,e\n", "[<abc>,<d>,<e>]", TestName = "Text after closing quote")]
    [TestCase("A,B,C\n\"ab\" ,d,e\n", "[<ab >,<d>,<e>]", TestName = "Space after closing quote")]
    [TestCase("A,B,C\n \"ab\",d,e\n", "[< \"ab\">,<d>,<e>]", TestName = "Space before opening quote")]
    [TestCase("A,B,C\n\"abc,d,e\n", "throws IndexOutOfRangeException", TestName = "Unclosed quote runs to the end")]
    [TestCase("A,B,C\nx,y,z,w\n", "[<x>,<y>,<z>]", TestName = "Extra fields ignored")]
    [TestCase("A,B,C,\nx,y,z,\n", "[<x>,<y>,<z>]", TestName = "Trailing separator")]
    [TestCase("A,B,C\nx,y\n", "throws IndexOutOfRangeException", TestName = "Missing fields")]
    [TestCase("﻿A,B,C\nx,y,z\n", "[<x>,<y>,<z>]", TestName = "Byte order mark")]
    [TestCase("", "", TestName = "Empty input")]
    [TestCase("﻿", "", TestName = "Byte order mark only")]
    [TestCase("A,B,C\n", "", TestName = "Header only")]
    [TestCase("A,B,C\nx,,\n", "[<x>,<>,<>]", TestName = "Empty fields")]
    [TestCase("A,B,C\n,,\n", "[<>,<>,<>]", TestName = "Only separators")]
    [TestCase("A,B,C\n\"\",y,z\n", "[<>,<y>,<z>]", TestName = "Quoted empty field")]
    [TestCase(" A ,B,C\nx,y,z\n", "[null,<y>,<z>]", TestName = "Header spaces kept")]
    [TestCase("a,b,c\nx,y,z\n", "[<x>,<y>,<z>]", TestName = "Header case ignored")]
    [TestCase("A,A,B\n1,2,3\n", "[<1>,<3>,null]", TestName = "Duplicate header first wins")]
    [TestCase("\"A\",\"B\",\"C\"\nx,y,z\n", "[<x>,<y>,<z>]", TestName = "Quoted header")]
    [TestCase("C,A,B\nz,x,y\n", "[<x>,<y>,<z>]", TestName = "Columns in any order")]
    [TestCase("A,B,C\nx\ty,y,z\n", "[<x\ty>,<y>,<z>]", TestName = "Tab inside field")]
    public async Task Read_MatchesCsvHelper(string csv, string expected)
    {
        Assert.That(await ReadAll(csv), Is.EqualTo(expected));
    }

    [Test]
    public async Task Read_UsesTheCultureListSeparator()
    {
        Assert.That(await ReadAll("A;B;C\nx;y,1;z\n", new CultureInfo("de-DE")), Is.EqualTo("[<x>,<y,1>,<z>]"));
    }

    [Test]
    public async Task Read_QuotedFieldsAcrossTheReadBuffer()
    {
        // Fields, escaped quotes and CRLF line ends that straddle the reader's 8 KB buffer.
        var big = new string('q', 8190);
        var csv = $"A,B,C\r\n\"{big}\"\"x\",\"a\r\nb\",z\r\n" + string.Concat(Enumerable.Range(0, 3000).Select(i => $"{i},\"v,{i}\",w\r\n"));

        var rows = await new ExcelConverter<CsvRow>().ReadCsvAsync(new MemoryStream(Encoding.UTF8.GetBytes(csv)));

        Assert.That(rows, Has.Count.EqualTo(3001));
        Assert.That(rows[0].A, Is.EqualTo(big + "\"x"));
        Assert.That(rows[0].B, Is.EqualTo("a\r\nb"));
        Assert.That(rows[2999].A, Is.EqualTo("2998"));
        Assert.That(rows[2999].B, Is.EqualTo("v,2998"));
        Assert.That(rows[3000].C, Is.EqualTo("w"));
    }

    [Test]
    public async Task Read_ForwardOnlyStream()
    {
        var bytes = Encoding.UTF8.GetBytes("A,B,C\nx,y,z\n");

        var rows = await new ExcelConverter<CsvRow>().ReadCsvAsync(new ReadOnlyForwardStream(new MemoryStream(bytes)));

        Assert.That(rows.Single().C, Is.EqualTo("z"));
    }

    [Test]
    public async Task Read_MissingFields_MessageNamesTheRecord()
    {
        var e = await Assert.ThrowsAsync<IndexOutOfRangeException>(() =>
            new ExcelConverter<CsvRow>().ReadCsvAsync(new MemoryStream("A,B,C\nx,y,z\nq,r\n"u8.ToArray())));

        Assert.That(e!.Message, Is.EqualTo("CSV record 3 has 2 fields, but the header has 3"));
    }

    private static async Task<string> WriteAll(CsvRow[] rows, CultureInfo? culture = null)
    {
        var converter = new ExcelConverter<CsvRow>();
        if (culture != null) converter.CultureInfo = culture;
        var stream = new MemoryStream();
        await converter.WriteCsvAsync(stream, rows);
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    [Test]
    public async Task Write_MatchesCsvHelper()
    {
        Assert.That(await WriteAll([
                new() { A = "Smith, John", B = "He said \"hi\"", C = "multi\nline" },
                new() { A = " Lead", B = "Trail ", C = "\tTab" }]),
            Is.EqualTo("A,B,C\r\n\"Smith, John\",\"He said \"\"hi\"\"\",\"multi\nline\"\r\n\" Lead\",\"Trail \",\"\tTab\"\r\n"));

        Assert.That(await WriteAll([new() { A = "", B = null!, C = "x" }]), Is.EqualTo("A,B,C\r\n,,x\r\n"));
        Assert.That(await WriteAll([new() { A = "a\rb", B = "c\r\nd", C = "  " }]), Is.EqualTo("A,B,C\r\n\"a\rb\",\"c\r\nd\",\"  \"\r\n"));
        Assert.That(await WriteAll([new() { A = "a b", B = "x", C = "y" }]), Is.EqualTo("A,B,C\r\na b,x,y\r\n"));
        Assert.That(await WriteAll([]), Is.EqualTo("A,B,C\r\n"));
    }

    [Test]
    public async Task Write_UsesTheCultureListSeparator()
    {
        Assert.That(await WriteAll([new() { A = "1,5", B = "a;b", C = "c" }], new CultureInfo("de-DE")),
            Is.EqualTo("A;B;C\r\n1,5;\"a;b\";c\r\n"));
    }

    [Test]
    public async Task WriteThenRead_RoundTripsAwkwardValues()
    {
        CsvRow[] rows =
        [
            new() { A = "a,b", B = "\"quoted\"", C = "line\r\nbreak" },
            new() { A = " lead", B = "trail ", C = "" },
            new() { A = "\"", B = "\"\"", C = "x\ry" }
        ];
        var converter = new ExcelConverter<CsvRow>();
        var stream = new MemoryStream();
        await converter.WriteCsvAsync(stream, rows);

        var back = await converter.ReadCsvAsync(stream);

        Assert.That(back.Select(r => (r.A, r.B, r.C)), Is.EqualTo(rows.Select(r => (r.A, r.B, r.C))));
    }

    /// <summary>A readable stream that cannot seek, like a request body.</summary>
    private class ReadOnlyForwardStream(Stream inner) : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => throw new InvalidOperationException("Synchronous reads are not allowed");
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => inner.ReadAsync(buffer, offset, count, cancellationToken);
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => inner.ReadAsync(buffer, cancellationToken);
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
