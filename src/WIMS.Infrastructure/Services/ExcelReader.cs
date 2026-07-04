using System.Text;
using ClosedXML.Excel;
using WIMS.Application.Common.Interfaces;

namespace WIMS.Infrastructure.Services;

/// <summary>
/// قارئ Excel عبر ClosedXML. يطبّع الخلايا (R-02): إزالة الفراغات والمحارف غير المرئية
/// وتحويل الأرقام العربية-الهندية إلى لاتينية لتفادي فشل المطابقة.
/// </summary>
public sealed class ExcelReader : IExcelReader
{
    public ExcelSheetData? ReadSheet(byte[] content, string sheetName)
    {
        using var stream = new MemoryStream(content);
        using var workbook = new XLWorkbook(stream);

        var worksheet = workbook.Worksheets
            .FirstOrDefault(w => Normalize(w.Name).Equals(Normalize(sheetName), StringComparison.OrdinalIgnoreCase));
        if (worksheet is null)
            return null;

        var firstRow = worksheet.FirstRowUsed();
        if (firstRow is null)
            return new ExcelSheetData(worksheet.Name, [], []);

        // العناوين من الصف الأول.
        var headers = new List<string>();
        var headerColumns = new List<(int Column, string Header)>();
        foreach (var cell in firstRow.CellsUsed())
        {
            var header = Normalize(cell.GetString());
            if (string.IsNullOrEmpty(header)) continue;
            headers.Add(header);
            headerColumns.Add((cell.Address.ColumnNumber, header));
        }

        var rows = new List<ExcelRowData>();
        var lastRow = worksheet.LastRowUsed()!.RowNumber();
        for (var r = firstRow.RowNumber() + 1; r <= lastRow; r++)
        {
            var row = worksheet.Row(r);
            var cells = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var (column, header) in headerColumns)
                cells[header] = Normalize(row.Cell(column).GetString());

            rows.Add(new ExcelRowData(r, cells));
        }

        return new ExcelSheetData(worksheet.Name, headers, rows);
    }

    /// <summary>Trim + إزالة المحارف غير المرئية + تحويل الأرقام العربية إلى لاتينية.</summary>
    private static string Normalize(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;

        var sb = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            // إزالة المحارف غير المرئية (Zero-width / BOM / اتجاهية).
            if (ch is '​' or '‌' or '‍' or '‎' or '‏' or '﻿')
                continue;

            // الأرقام العربية-الهندية (٠-٩).
            if (ch is >= '٠' and <= '٩') sb.Append((char)('0' + (ch - '٠')));
            // الأرقام الفارسية (۰-۹).
            else if (ch is >= '۰' and <= '۹') sb.Append((char)('0' + (ch - '۰')));
            else sb.Append(ch);
        }

        return sb.ToString().Trim();
    }
}
