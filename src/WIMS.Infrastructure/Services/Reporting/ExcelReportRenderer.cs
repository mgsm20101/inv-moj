using ClosedXML.Excel;
using WIMS.Application.Features.Reports;

namespace WIMS.Infrastructure.Services.Reporting;

/// <summary>يُصيّر <see cref="ReportDocument"/> إلى Excel عربي RTL عبر ClosedXML.</summary>
internal static class ExcelReportRenderer
{
    public static byte[] Render(ReportDocument doc)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("التقرير");
        ws.RightToLeft = true;

        var colCount = Math.Max(doc.Columns.Count, 1);
        var row = 1;

        // ── الترويسة الرسمية ──
        ws.Cell(row, 1).Value = "وزارة العدل — نظام إدارة المخازن (WIMS)";
        ws.Range(row, 1, row, colCount).Merge().Style.Font.SetBold().Font.SetFontSize(11);
        row++;

        ws.Cell(row, 1).Value = doc.Title;
        var titleRange = ws.Range(row, 1, row, colCount).Merge();
        titleRange.Style.Font.SetBold().Font.SetFontSize(14);
        titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        row++;

        if (!string.IsNullOrWhiteSpace(doc.Subtitle))
        {
            ws.Cell(row, 1).Value = doc.Subtitle;
            var r = ws.Range(row, 1, row, colCount).Merge();
            r.Style.Font.SetFontSize(11).Font.SetFontColor(XLColor.FromHtml("#555555"));
            r.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            row++;
        }

        foreach (var m in doc.Meta)
        {
            ws.Cell(row, 1).Value = $"{m.Label}: {m.Value}";
            ws.Range(row, 1, row, colCount).Merge().Style.Font.SetFontSize(9).Font.SetItalic();
            row++;
        }

        ws.Cell(row, 1).Value = $"تاريخ التوليد: {doc.GeneratedAt:yyyy-MM-dd HH:mm}";
        ws.Range(row, 1, row, colCount).Merge().Style.Font.SetFontSize(9).Font.SetFontColor(XLColor.Gray);
        row += 2;

        // ── ترويسة الجدول ──
        var headerRow = row;
        for (var c = 0; c < doc.Columns.Count; c++)
        {
            var cell = ws.Cell(headerRow, c + 1);
            cell.Value = doc.Columns[c].Header;
            cell.Style.Font.SetBold().Font.SetFontColor(XLColor.White);
            cell.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#1F3A5F"));
            cell.Style.Alignment.Horizontal = Map(doc.Columns[c].Align);
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }
        row++;

        // ── الصفوف ──
        foreach (var dataRow in doc.Rows)
        {
            for (var c = 0; c < doc.Columns.Count; c++)
            {
                var cell = ws.Cell(row, c + 1);
                cell.Value = c < dataRow.Count ? dataRow[c] : string.Empty;
                cell.Style.Alignment.Horizontal = Map(doc.Columns[c].Align);
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Hair;
                cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#B8C4D4");
            }
            row++;
        }

        // ── الإجماليات ──
        if (doc.Totals is { Count: > 0 } totals)
        {
            for (var c = 0; c < doc.Columns.Count; c++)
            {
                var cell = ws.Cell(row, c + 1);
                cell.Value = c < totals.Count ? totals[c] : string.Empty;
                cell.Style.Font.SetBold();
                cell.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#E8EEF6"));
                cell.Style.Alignment.Horizontal = Map(doc.Columns[c].Align);
            }
            row++;
        }

        if (doc.Columns.Count > 0)
        {
            ws.SheetView.FreezeRows(headerRow);
            ws.Columns(1, doc.Columns.Count).AdjustToContents(headerRow, row, 8d, 45d);
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static XLAlignmentHorizontalValues Map(ReportAlignment align) => align switch
    {
        ReportAlignment.Center => XLAlignmentHorizontalValues.Center,
        ReportAlignment.Left => XLAlignmentHorizontalValues.Left,
        _ => XLAlignmentHorizontalValues.Right,
    };
}
