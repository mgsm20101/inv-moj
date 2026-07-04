using System.Globalization;
using System.Text;
using QuestPDF.Infrastructure;
using WIMS.Application.Common.Interfaces;
using WIMS.Application.Features.Reports;

namespace WIMS.Infrastructure.Services.Reporting;

/// <summary>
/// يوجّه تصدير التقرير إلى مُصيّر PDF أو Excel ويبني اسم الملف ونوع المحتوى.
/// يُفعّل ترخيص QuestPDF المجتمعي مرة واحدة.
/// </summary>
public sealed class ReportExporter : IReportExporter
{
    static ReportExporter()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public ReportFile Export(ReportDocument document, ReportFormat format)
    {
        var stamp = document.GeneratedAt.ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture);
        var baseName = $"{Sanitize(document.Title)}-{stamp}";

        return format switch
        {
            ReportFormat.Excel => new ReportFile(
                ExcelReportRenderer.Render(document),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{baseName}.xlsx"),
            _ => new ReportFile(
                PdfReportRenderer.Render(document),
                "application/pdf",
                $"{baseName}.pdf"),
        };
    }

    /// <summary>ينظّف عنوان التقرير ليصلح اسم ملف (يُبقي العربية والأرقام ويستبدل الفواصل بشرطة).</summary>
    private static string Sanitize(string title)
    {
        var sb = new StringBuilder(title.Length);
        foreach (var ch in title)
        {
            if (char.IsLetterOrDigit(ch)) sb.Append(ch);
            else if (ch is ' ' or '-' or '_') sb.Append('-');
        }
        var result = sb.ToString().Trim('-');
        return string.IsNullOrEmpty(result) ? "report" : result;
    }
}
