# Excel and CSV

`Halifax.Excel` reads and writes `.xlsx`, `.xls` and CSV files as lists of your own types.

```bash
dotnet add package Halifax.Excel
```

## Converters

One `ExcelConverter<T>` per record type holds the column mappings and exclusions; it can be reused for any number of reads and writes.

```csharp
var converter = new ExcelConverter<Person>();
converter.AddMapping("Full Name", p => p.Name);   // header text <-> property
converter.Exclude(p => p.InternalId);             // not written, ignored when read
```

Properties without a mapping use their own name as the header.

## Reading

```csharp
// Format picked from the content type (.xlsx and .xls are told apart from the file itself)
var people = await converter.ReadAsync(file.OpenReadStream(), file.ContentType);

var fromFirstSheet = converter.ReadExcel(stream);
var fromNamedSheet = converter.ReadExcel(stream, "People");
var fromCsv = await converter.ReadCsvAsync(stream);
```

Excel reading:

- The first row is the header. Headers match mappings and property names case-insensitively, ignoring surrounding spaces; columns that match nothing are ignored.
- Rows whose matched cells are all blank are skipped. A blank cell leaves its property at its initial value.
- Records with only a constructor (positional records) are filled through the constructor.
- Cells are read from their stored values: numbers into number, `bool`, enum and date properties; dates into `DateTime`, `DateOnly`, `TimeOnly` and `DateTimeOffset`; formulas give their last calculated result; text is parsed with the converter's `CultureInfo`.
- A number read into a `string` keeps simple formats: `000000` keeps leading zeros, `#,##0.00` keeps separators. Other formats (currency, fractions) come back as the plain number.
- A cell that cannot be converted throws `ExcelConversionException`, with `Row` and `Column` as Excel shows them, `Value` and `TargetType`. Its message can go straight back to whoever uploaded the file.

CSV reading:

- The separator is the converter culture's list separator: a comma for the invariant culture, `;` for many European ones.
- Records end at `\r\n`, `\n` or `\r`; empty lines are skipped. A field starting with `"` is quoted: separators and line breaks inside it are data and `""` is a quote. Nothing is trimmed.
- Mapped headers match exactly; property names match case-insensitively. Extra fields are ignored; a record with fewer fields than the header throws `IndexOutOfRangeException`.
- Values that do not convert to their property's type are skipped with a trace warning.
- The stream is read asynchronously in chunks, so large uploads and request bodies are fine.

The CSV reader and writer are part of Halifax.Excel itself (from 6.2.0, no CsvHelper).

## Writing

```csharp
var stream = new MemoryStream();
await converter.WriteExcelAsync(stream, people, "People");
return File(stream, ExcelContentType.Xlsx, "People.xlsx");   // FileStreamResult disposes it

await converter.WriteCsvAsync(csvStream, people);
```

- Writes leave the stream open and rewind a seekable stream to where the output starts, ready to return, upload or attach.
- One column per public property, in declaration order; the header row is bold on a light-yellow fill.
- Numbers and `bool` are written as Excel values. `DateTime`, `DateTimeOffset` and `DateOnly` are written as dates, shown with `DateTimeFormat` (default `yyyy-mm-dd hh:mm:ss`) and `DateFormat` (default `yyyy-mm-dd`). Anything else is written as text.
- Column widths follow the longest value: `WidthToStringLengthFactor` (default 300 per character), clamped to `MinCellWidth` (2700) and `MaxCellWidth` (9000).
- CSV uses the converter's `CultureInfo` for values and its list separator (a comma for the invariant culture).

## Several sheets in one workbook

```csharp
using var writer = new ExcelWorkbookWriter();
writer.AddSheet("People", people, peopleConverter)
      .AddSheet("Orders", orders);   // no converter: property names and defaults
writer.Write(stream);
```

Each sheet uses its own converter for mappings, exclusions, widths and date formats. Sheet names must be unique, at most 31 characters, and free of `: \ / ? * [ ]`.

## Content types

`ExcelContentType.Xlsx`, `ExcelContentType.Xls` and `ExcelContentType.Csv` hold the MIME types for responses and attachments.

## SkiaSharp

Halifax.Excel neither ships nor loads SkiaSharp. If your own code uses NPOI directly, note that NPOI 2.8 needs SkiaSharp for `DataFormatter`, for `ToString()` on .xls cells and for `AutoSizeColumn`, but does not ship it, so those calls fail at runtime with `FileNotFoundException`. `ExcelCell.GetText(cell)` is a SkiaSharp-free replacement for `cell?.ToString()`.
