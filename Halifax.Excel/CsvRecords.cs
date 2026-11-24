using System.Text;

namespace Halifax.Excel;

/// <summary>
/// Reads CSV records (RFC 4180 with the lenient rules CsvHelper applied in earlier versions) from a text reader,
/// asynchronously and without loading the whole file.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>Records end at <c>\r\n</c>, <c>\n</c> or <c>\r</c>; completely empty lines are skipped.</item>
/// <item>A field starting with a quote is quoted: separators and line breaks inside it are data, <c>""</c> is a quote.
/// Text after the closing quote is appended as is (<c>"ab"c</c> reads <c>abc</c>); a missing closing quote runs to the
/// end of the input.</item>
/// <item>A quote anywhere else is an ordinary character. Nothing is trimmed.</item>
/// </list>
/// </remarks>
internal sealed class CsvRecordReader(TextReader reader, char separator)
{
    private const int eof = -1;
    private readonly char[] buffer = new char[8192];
    private readonly StringBuilder field = new();
    private int position;
    private int length;

    /// <summary>Reads the next non-empty record, or returns <see langword="null"/> at the end of the input.</summary>
    public async ValueTask<List<string>?> ReadAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            var c = await NextAsync(cancellationToken);
            switch (c)
            {
                case eof:
                    return null;
                case '\n':
                    continue;
                case '\r':
                    await SkipLineFeedAsync(cancellationToken);
                    continue;
                default:
                    return await ReadFieldsAsync(c, cancellationToken);
            }
        }
    }

    private async ValueTask<List<string>> ReadFieldsAsync(int c, CancellationToken cancellationToken)
    {
        var fields = new List<string>();

        while (true)
        {
            field.Clear();

            if (c == '"')
            {
                while (true)
                {
                    c = await NextAsync(cancellationToken);
                    if (c == eof)
                    {
                        fields.Add(field.ToString());
                        return fields;
                    }

                    if (c == '"')
                    {
                        if (await PeekAsync(cancellationToken) == '"')
                        {
                            await NextAsync(cancellationToken);
                            field.Append('"');
                            continue;
                        }

                        break;
                    }

                    field.Append((char)c);
                }

                c = await NextAsync(cancellationToken);
            }

            while (c != eof && c != separator && c != '\r' && c != '\n')
            {
                field.Append((char)c);
                c = await NextAsync(cancellationToken);
            }

            fields.Add(field.ToString());

            if (c == separator)
            {
                c = await NextAsync(cancellationToken);
                continue;
            }

            if (c == '\r')
            {
                await SkipLineFeedAsync(cancellationToken);
            }

            return fields;
        }
    }

    private async ValueTask SkipLineFeedAsync(CancellationToken cancellationToken)
    {
        if (await PeekAsync(cancellationToken) == '\n')
        {
            await NextAsync(cancellationToken);
        }
    }

    private async ValueTask<int> NextAsync(CancellationToken cancellationToken)
    {
        if (position == length && !await FillAsync(cancellationToken))
        {
            return eof;
        }

        return buffer[position++];
    }

    private async ValueTask<int> PeekAsync(CancellationToken cancellationToken)
    {
        if (position == length && !await FillAsync(cancellationToken))
        {
            return eof;
        }

        return buffer[position];
    }

    private async ValueTask<bool> FillAsync(CancellationToken cancellationToken)
    {
        length = await reader.ReadAsync(buffer.AsMemory(), cancellationToken);
        position = 0;
        return length > 0;
    }
}

/// <summary>Formats CSV records the way earlier versions wrote them through CsvHelper.</summary>
internal static class CsvRecordWriter
{
    /// <summary>Line ending after every record, header included.</summary>
    public const string NewLine = "\r\n";

    /// <summary>
    /// Joins fields into one record line. A field is quoted, with quotes doubled, when it contains the separator,
    /// a quote or a line break, or starts or ends with whitespace (only a space when <paramref name="spaceEdgesOnly"/>
    /// is set, as CsvHelper's defaults do); <see langword="null"/> writes an empty field.
    /// </summary>
    public static string Format(IEnumerable<string?> fields, char separator, bool spaceEdgesOnly = false)
    {
        var line = new StringBuilder();
        var first = true;

        foreach (var value in fields)
        {
            if (!first)
            {
                line.Append(separator);
            }

            first = false;

            if (string.IsNullOrEmpty(value))
            {
                continue;
            }

            if (NeedsQuotes(value, separator, spaceEdgesOnly))
            {
                line.Append('"').Append(value.Replace("\"", "\"\"")).Append('"');
            }
            else
            {
                line.Append(value);
            }
        }

        return line.Append(NewLine).ToString();
    }

    private static bool NeedsQuotes(string value, char separator, bool spaceEdgesOnly) =>
        value.AsSpan().IndexOfAny(separator, '"', '\r') >= 0 ||
        value.Contains('\n') ||
        (spaceEdgesOnly
            ? value[0] == ' ' || value[^1] == ' '
            : char.IsWhiteSpace(value[0]) || char.IsWhiteSpace(value[^1]));
}
