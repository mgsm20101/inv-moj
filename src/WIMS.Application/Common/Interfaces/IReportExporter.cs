using WIMS.Application.Features.Reports;

namespace WIMS.Application.Common.Interfaces;

/// <summary>
/// مُصدِّر التقارير — يحوّل <see cref="ReportDocument"/> إلى PDF (QuestPDF RTL) أو Excel (ClosedXML).
/// يُنفَّذ في طبقة Infrastructure لأن المكتبتين تفصيلا بنية تحتية.
/// </summary>
public interface IReportExporter
{
    ReportFile Export(ReportDocument document, ReportFormat format);
}
