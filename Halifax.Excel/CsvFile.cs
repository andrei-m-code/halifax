using System.Globalization;
using System.Runtime.CompilerServices;

namespace Halifax.Excel;

/// <summary>
/// Reads and writes CSV as plain records, for rows that have no C# type of their own: dictionaries, or <c>dynamic</c>
/// rows such as Dapper's (which are dictionaries too), or your own column mapping on top of the raw fields. For typed
/// records, use <see cref="ExcelConverter{TObject}.ReadCsvAsync"/> and <see cref="ExcelConverter{TObject}.WriteCsvAsync"/>.
/// </summary>
/// <remarks>
/// The output follows CsvHelper's defaults for dynamic records, so it can replace
/// <c>new CsvWriter(writer, culture).WriteRecordsAsync(rows)</c> byte for byte:
/// <list type="bullet">
/// <item>The header is the first row's keys, in order; with no rows nothing is written, not even a header.</item>
/// <item>The separator is the culture's list separator (a comma for the invariant culture); every record ends with <c>\r\n</c>.</item>
/// <item>A field is quoted, quotes doubled, when it contains the separator, a quote or a line break, or starts or ends with a space.</item>
/// <item><see langword="null"/> is an empty field; a <c>byte[]</c> is <c>0x</c> and upper-case hex; anything formattable uses the culture.</item>
/// </list>
/// The stream gets UTF-8 without a byte order mark, and is left open and, when seekable, rewound.
/// </remarks>
/// <example>
/// <code>
/// var rows = await connection.QueryAsync(sql);   // Dapper: IEnumerable&lt;dynamic&gt;
/// await using var file = File.Create(path);
/// await CsvFile.WriteAsync(file, rows.Cast&lt;IDictionary&lt;string, object?&gt;&gt;());
/// </code>
/// </example>
public static class CsvFile
{
    /// <summary>
    /// Reads every CSV record as its list of fields, the header row included as the first record.
    /// </summary>
    /// <param name="stream">The CSV stream, read asynchronously in chunks. A UTF-8 byte order mark is skipped. It is left open.</param>
    /// <param name="culture">Picks the separator (its list separator; a comma for the invariant culture). Default is
    /// <see cref="CultureInfo.InvariantCulture"/>.</param>
    /// <param name="cancellationToken">Token that cancels the read.</param>
    /// <returns>The records in file order; nothing for an empty input.</returns>
    /// <remarks>
    /// The same parser <see cref="ExcelConverter{TObject}.ReadCsvAsync"/> uses, which reads exactly as CsvHelper did:
    /// records end at <c>\r\n</c>, <c>\n</c> or <c>\r</c>; empty lines are skipped; a field starting with a quote is
    /// quoted (separators and line breaks inside it are data, <c>""</c> is a quote); nothing is trimmed. Records keep
    /// their own field count, so check it against the header where a short row matters.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <see langword="null"/>.</exception>
    public static async IAsyncEnumerable<IReadOnlyList<string>> ReadAsync(
        Stream stream,
        CultureInfo? culture = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var text = new StreamReader(stream, leaveOpen: true);
        var reader = new CsvRecordReader(text, Separator(culture ?? CultureInfo.InvariantCulture));

        while (await reader.ReadAsync(cancellationToken) is { } fields)
        {
            yield return fields;
        }
    }

    /// <summary>
    /// Writes dictionary rows as CSV: the first row's keys as the header, then one record per row.
    /// </summary>
    /// <param name="stream">The stream that receives the CSV. It is left open and, when seekable, rewound to where the CSV starts.</param>
    /// <param name="rows">The rows, enumerated once. Keys a row lacks are written as empty fields; keys the first row lacks are ignored.</param>
    /// <param name="culture">Formats values and picks the separator. Default is <see cref="CultureInfo.InvariantCulture"/>.</param>
    /// <param name="cancellationToken">Token that cancels the write.</param>
    /// <returns>A task that completes once the CSV is written and flushed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> or <paramref name="rows"/> is <see langword="null"/>.</exception>
    public static async Task WriteAsync(
        Stream stream,
        IEnumerable<IDictionary<string, object?>> rows,
        CultureInfo? culture = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(rows);

        await WriteRecordsAsync(stream, Records(rows), culture, cancellationToken);

        static IEnumerable<IEnumerable<object?>> Records(IEnumerable<IDictionary<string, object?>> rows)
        {
            List<string>? header = null;

            foreach (var row in rows)
            {
                if (header == null)
                {
                    header = row.Keys.ToList();
                    yield return header;
                }

                yield return header.Select(key => row.TryGetValue(key, out var value) ? value : null);
            }
        }
    }

    /// <summary>
    /// Writes a header and rows of values as CSV.
    /// </summary>
    /// <param name="stream">The stream that receives the CSV. It is left open and, when seekable, rewound to where the CSV starts.</param>
    /// <param name="header">The column names, written as the first record.</param>
    /// <param name="rows">The rows of values, in column order, enumerated once.</param>
    /// <param name="culture">Formats values and picks the separator. Default is <see cref="CultureInfo.InvariantCulture"/>.</param>
    /// <param name="cancellationToken">Token that cancels the write.</param>
    /// <returns>A task that completes once the CSV is written and flushed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/>, <paramref name="header"/> or <paramref name="rows"/> is <see langword="null"/>.</exception>
    public static Task WriteAsync(
        Stream stream,
        IReadOnlyList<string> header,
        IEnumerable<IEnumerable<object?>> rows,
        CultureInfo? culture = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(header);
        ArgumentNullException.ThrowIfNull(rows);

        return WriteRecordsAsync(stream, rows.Prepend(header), culture, cancellationToken);
    }

    private static async Task WriteRecordsAsync(
        Stream stream,
        IEnumerable<IEnumerable<object?>> records,
        CultureInfo? culture,
        CancellationToken cancellationToken)
    {
        culture ??= CultureInfo.InvariantCulture;
        var separator = Separator(culture);
        var start = stream.CanSeek ? stream.Position : 0;

        await using (var writer = new StreamWriter(stream, leaveOpen: true))
        {
            foreach (var record in records)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var line = CsvRecordWriter.Format(record.Select(value => Format(value, culture)), separator, spaceEdgesOnly: true);
                await writer.WriteAsync(line.AsMemory(), cancellationToken);
            }

            await writer.FlushAsync(cancellationToken);
        }

        if (stream.CanSeek)
        {
            stream.Position = start;
        }
    }

    private static string? Format(object? value, CultureInfo culture) => value switch
    {
        null => null,
        string text => text,
        byte[] bytes => "0x" + Convert.ToHexString(bytes),
        IFormattable formattable => formattable.ToString(null, culture),
        _ => value.ToString()
    };

    // The culture's list separator (a comma for the invariant culture), falling back to a comma when that
    // separator is not a single ASCII character. Same rule as ExcelConverter.
    private static char Separator(CultureInfo culture)
    {
        var listSeparator = culture.TextInfo.ListSeparator;
        return listSeparator.Length == 1 && char.IsAscii(listSeparator[0]) ? listSeparator[0] : ',';
    }
}
