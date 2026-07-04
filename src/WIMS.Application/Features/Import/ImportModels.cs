namespace WIMS.Application.Features.Import;

/// <summary>خطأ في صف استيراد — يحمل رقم الصف والعمود والرسالة (تقرير أخطاء صفّي).</summary>
public sealed record ImportRowError(int RowNumber, string Column, string Message);

/// <summary>حصيلة الاستيراد: إجماليات + تقرير أخطاء + هل تم الاعتماد (Commit) أم معاينة.</summary>
public sealed record ImportResult(
    int TotalRows,
    int ValidRows,
    int ImportedCount,
    bool Committed,
    IReadOnlyList<ImportRowError> Errors)
{
    public bool HasErrors => Errors.Count > 0;
}
