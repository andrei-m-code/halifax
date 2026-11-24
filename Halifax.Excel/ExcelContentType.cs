namespace Halifax.Excel;

/// <summary>
/// MIME content types for the files <see cref="ExcelConverter{TObject}"/> reads and writes,
/// e.g. for a <c>FileResult</c> or an email attachment.
/// </summary>
public static class ExcelContentType
{
    /// <summary>Excel workbook (.xlsx), the format every write produces.</summary>
    public const string Xlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    /// <summary>Legacy Excel workbook (.xls). Readable; writes always produce .xlsx.</summary>
    public const string Xls = "application/vnd.ms-excel";

    /// <summary>Comma-separated values (.csv).</summary>
    public const string Csv = "text/csv";
}
