namespace WIMS.Application.Features.Reports;

/// <summary>محاذاة عمود التقرير (RTL: الافتراضي يمين).</summary>
public enum ReportAlignment
{
    Right = 0,
    Center = 1,
    Left = 2,
}

/// <summary>صيغة تصدير التقرير.</summary>
public enum ReportFormat
{
    Pdf = 0,
    Excel = 1,
}

/// <summary>تعريف عمود في التقرير الجدولي.</summary>
/// <param name="Header">عنوان العمود (عربي).</param>
/// <param name="Align">محاذاة الخلايا.</param>
/// <param name="Width">الوزن النسبي لعرض العمود (يُستخدم في PDF).</param>
public sealed record ReportColumn(string Header, ReportAlignment Align = ReportAlignment.Right, float Width = 1f);

/// <summary>
/// نموذج تقرير جدولي محايد للصيغة — تُنتجه معالِجات الاستعلام وتستهلكه مُصدِّرات PDF/Excel.
/// الخلايا نصوص مُنسَّقة مسبقاً (أرقام/تواريخ) كي يبقى المُصدِّر عرضياً بحتاً.
/// </summary>
public sealed record ReportDocument
{
    /// <summary>عنوان التقرير الرسمي.</summary>
    public required string Title { get; init; }

    /// <summary>عنوان فرعي اختياري (مثل اسم المخزن/النطاق).</summary>
    public string? Subtitle { get; init; }

    /// <summary>بيانات ترويسة (تسمية/قيمة) — نطاق التاريخ، المخزن، مُنشئ التقرير...</summary>
    public IReadOnlyList<ReportMeta> Meta { get; init; } = [];

    public IReadOnlyList<ReportColumn> Columns { get; init; } = [];

    /// <summary>صفوف البيانات — طول كل صف = عدد الأعمدة.</summary>
    public IReadOnlyList<IReadOnlyList<string>> Rows { get; init; } = [];

    /// <summary>صف إجماليات اختياري — طوله = عدد الأعمدة.</summary>
    public IReadOnlyList<string>? Totals { get; init; }

    /// <summary>لحظة توليد التقرير (وقت الخادم المحلي).</summary>
    public DateTime GeneratedAt { get; init; }
}

/// <summary>عنصر ترويسة (تسمية/قيمة).</summary>
public sealed record ReportMeta(string Label, string Value);

/// <summary>ملف مُصدَّر جاهز للإرجاع عبر HTTP.</summary>
public sealed record ReportFile(byte[] Content, string ContentType, string FileName);
