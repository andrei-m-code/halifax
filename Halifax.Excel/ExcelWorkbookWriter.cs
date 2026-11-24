using NPOI.XSSF.UserModel;

namespace Halifax.Excel;

/// <summary>
/// Builds one Excel (.xlsx) workbook out of several sheets, each with its own record type.
/// </summary>
/// <remarks>
/// Each sheet is written exactly as <see cref="ExcelConverter{TObject}.WriteExcelAsync"/> writes its single
/// sheet: the converter passed to <see cref="AddSheet{TObject}"/> supplies that sheet's column mappings,
/// exclusions, column widths and date formats.
/// </remarks>
/// <example>
/// <code>
/// var users = new ExcelConverter&lt;UserReport&gt;();
/// users.AddMapping("Customer Name", r =&gt; r.Name);
///
/// using var writer = new ExcelWorkbookWriter();
/// writer.AddSheet("Users", userRows, users)
///       .AddSheet("Searches", searchRows);
///
/// using var stream = new MemoryStream();
/// writer.Write(stream);   // stream stays open, positioned at the start of the workbook
/// </code>
/// </example>
public sealed class ExcelWorkbookWriter : IDisposable
{
    private readonly XSSFWorkbook workbook = new();
    private readonly ExcelStyles styles;

    /// <summary>
    /// Creates an empty workbook.
    /// </summary>
    public ExcelWorkbookWriter()
    {
        styles = new ExcelStyles(workbook);
    }

    /// <summary>
    /// The number of sheets added so far.
    /// </summary>
    public int SheetCount => workbook.NumberOfSheets;

    /// <summary>
    /// Adds a sheet with a header row and one row per record, after the sheets already added.
    /// </summary>
    /// <param name="sheetName">The worksheet name. Must be unique in the workbook, at most 31 characters,
    /// and free of <c>: \ / ? * [ ]</c>.</param>
    /// <param name="records">The records to write, one per row, in enumeration order. An empty sequence
    /// writes the header row only.</param>
    /// <param name="converter">Supplies the mappings, exclusions, column widths and date formats for this
    /// sheet. When omitted, property names become the headers and the defaults apply.</param>
    /// <typeparam name="TObject">The record type; its public properties become the columns.</typeparam>
    /// <returns>This writer, so sheets can be chained.</returns>
    /// <exception cref="ArgumentException">The sheet name is empty, invalid or already used.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="records"/> is <see langword="null"/>.</exception>
    public ExcelWorkbookWriter AddSheet<TObject>(
        string sheetName,
        IEnumerable<TObject> records,
        ExcelConverter<TObject>? converter = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sheetName);
        ArgumentNullException.ThrowIfNull(records);

        if (workbook.GetSheet(sheetName) != null)
        {
            throw new ArgumentException($"The workbook already has a sheet named '{sheetName}'", nameof(sheetName));
        }

        (converter ?? new ExcelConverter<TObject>()).WriteSheet(workbook, styles, sheetName, records);
        return this;
    }

    /// <summary>
    /// Writes the workbook to <paramref name="stream"/>. The stream is left open and, when seekable,
    /// rewound to where the workbook starts.
    /// </summary>
    /// <param name="stream">The stream that receives the workbook.</param>
    /// <exception cref="InvalidOperationException">No sheet has been added.</exception>
    public void Write(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (workbook.NumberOfSheets == 0)
        {
            throw new InvalidOperationException("Add at least one sheet before writing the workbook");
        }

        ExcelStyles.Write(workbook, stream);
    }

    /// <summary>
    /// Releases the workbook.
    /// </summary>
    public void Dispose() => workbook.Close();
}
