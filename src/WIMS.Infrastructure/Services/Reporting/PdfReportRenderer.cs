using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WIMS.Application.Features.Reports;

namespace WIMS.Infrastructure.Services.Reporting;

/// <summary>
/// يُصيّر <see cref="ReportDocument"/> إلى PDF عربي RTL بالنموذج الرسمي عبر QuestPDF.
/// الخط "Arial" (متوفّر على Windows/IIS) لدعم المحارف العربية.
/// </summary>
internal static class PdfReportRenderer
{
    private const string Font = "Arial";
    private static readonly Color HeaderBg = Color.FromHex("#1F3A5F");
    private static readonly Color HeaderText = Colors.White;
    private static readonly Color TotalsBg = Color.FromHex("#E8EEF6");
    private static readonly Color Border = Color.FromHex("#B8C4D4");

    public static byte[] Render(ReportDocument doc)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(18);
                page.DefaultTextStyle(t => t.FontFamily(Font).FontSize(9));
                page.ContentFromRightToLeft();

                page.Header().Element(h => ComposeHeader(h, doc));
                page.Content().PaddingVertical(6).Element(c => ComposeTable(c, doc));
                page.Footer().Element(ComposeFooter);
            });
        }).GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, ReportDocument doc)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("وزارة العدل").FontSize(11).Bold();
                    c.Item().Text("نظام إدارة المخازن والمخزون (WIMS)").FontSize(8).FontColor(Colors.Grey.Darken1);
                });
                row.RelativeItem().AlignLeft().Text(t =>
                {
                    t.AlignLeft();
                    t.Span("تاريخ التوليد: ").FontSize(8).FontColor(Colors.Grey.Darken1);
                    t.Span(doc.GeneratedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)).FontSize(8);
                });
            });

            col.Item().PaddingTop(4).LineHorizontal(1).LineColor(HeaderBg);

            col.Item().PaddingTop(6).AlignCenter().Text(doc.Title).FontSize(14).Bold().FontColor(HeaderBg);
            if (!string.IsNullOrWhiteSpace(doc.Subtitle))
                col.Item().AlignCenter().Text(doc.Subtitle).FontSize(10).FontColor(Colors.Grey.Darken2);

            if (doc.Meta.Count > 0)
            {
                col.Item().PaddingTop(4).Row(row =>
                {
                    foreach (var m in doc.Meta)
                    {
                        row.RelativeItem().Text(t =>
                        {
                            t.Span($"{m.Label}: ").FontSize(8).SemiBold().FontColor(Colors.Grey.Darken1);
                            t.Span(m.Value).FontSize(8);
                        });
                    }
                });
            }
        });
    }

    private static void ComposeTable(IContainer container, ReportDocument doc)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                foreach (var c in doc.Columns)
                    cols.RelativeColumn(c.Width);
            });

            // ترويسة الجدول.
            table.Header(header =>
            {
                foreach (var c in doc.Columns)
                {
                    header.Cell().Background(HeaderBg).Padding(4)
                        .AlignmentCell(c.Align)
                        .Text(c.Header).FontColor(HeaderText).SemiBold().FontSize(9);
                }
            });

            // الصفوف (تظليل متناوب).
            var rowIndex = 0;
            foreach (var row in doc.Rows)
            {
                var bg = rowIndex % 2 == 0 ? Colors.White : Color.FromHex("#F5F8FC");
                for (var i = 0; i < doc.Columns.Count; i++)
                {
                    var value = i < row.Count ? row[i] : string.Empty;
                    table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Border).Padding(3)
                        .AlignmentCell(doc.Columns[i].Align)
                        .Text(value).FontSize(8.5f);
                }
                rowIndex++;
            }

            // صف الإجماليات.
            if (doc.Totals is { Count: > 0 } totals)
            {
                for (var i = 0; i < doc.Columns.Count; i++)
                {
                    var value = i < totals.Count ? totals[i] : string.Empty;
                    table.Cell().Background(TotalsBg).Padding(4)
                        .AlignmentCell(doc.Columns[i].Align)
                        .Text(value).Bold().FontSize(9);
                }
            }
        });
    }

    private static void ComposeFooter(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text(t =>
            {
                t.Span("مستند آلي — نظام WIMS").FontSize(7).FontColor(Colors.Grey.Medium);
            });
            row.RelativeItem().AlignLeft().Text(t =>
            {
                t.AlignLeft();
                t.Span("صفحة ").FontSize(8);
                t.CurrentPageNumber().FontSize(8);
                t.Span(" / ").FontSize(8);
                t.TotalPages().FontSize(8);
            });
        });
    }

    /// <summary>يطبّق محاذاة الخلية حسب <see cref="ReportAlignment"/> (سياق RTL).</summary>
    private static IContainer AlignmentCell(this IContainer container, ReportAlignment align) => align switch
    {
        ReportAlignment.Center => container.AlignCenter(),
        ReportAlignment.Left => container.AlignLeft(),
        _ => container.AlignRight(),
    };
}
