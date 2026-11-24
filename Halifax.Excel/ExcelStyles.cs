using NPOI.SS.UserModel;

namespace Halifax.Excel;

/// <summary>
/// Cell styles for one workbook, created once and shared by every sheet written into it
/// (a workbook holds a limited number of styles).
/// </summary>
internal sealed class ExcelStyles(IWorkbook workbook)
{
    private readonly Dictionary<string, ICellStyle> formats = new(StringComparer.Ordinal);
    private ICellStyle? header;

    public ICellStyle Header => header ??= CreateHeader();

    public ICellStyle ForFormat(string format)
    {
        if (!formats.TryGetValue(format, out var style))
        {
            style = workbook.CreateCellStyle();
            style.DataFormat = workbook.CreateDataFormat().GetFormat(format);
            formats[format] = style;
        }

        return style;
    }

    private ICellStyle CreateHeader()
    {
        var style = workbook.CreateCellStyle();
        var font = workbook.CreateFont();
        font.IsBold = true;
        style.SetFont(font);
        style.FillForegroundColor = IndexedColors.LightYellow.Index;
        style.FillPattern = FillPattern.SolidForeground;
        return style;
    }

    /// <summary>
    /// Writes the workbook without closing <paramref name="stream"/>, then rewinds a seekable stream to where
    /// the workbook starts, so it can be read, uploaded or returned straight away.
    /// </summary>
    public static void Write(IWorkbook workbook, Stream stream)
    {
        var start = stream.CanSeek ? stream.Position : 0;
        workbook.Write(stream, true);
        stream.Flush();

        if (stream.CanSeek)
        {
            stream.Position = start;
        }
    }
}
