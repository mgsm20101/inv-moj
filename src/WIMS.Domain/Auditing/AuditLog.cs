namespace WIMS.Domain.Auditing;

/// <summary>
/// سجل تدقيق لكل عملية مؤثِّرة (FR-AUD-01..03): مَن، متى، أي كيان، القيمة قبل/بعد.
/// قابل للتعديل بصلاحية Audit.Edit مع توثيق مَن عدّل السجل ومتى (تعديل موثّق لا استبدال صامت).
/// لا يرث BaseEntity تجنّباً لتدقيق سجل التدقيق نفسه.
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>المستخدم الذي نفّذ العملية.</summary>
    public string? UserId { get; set; }
    public string? UserName { get; set; }

    public DateTime Timestamp { get; set; }

    /// <summary>اسم الكيان المتأثّر (مثل: Item).</summary>
    public string Entity { get; set; } = string.Empty;

    /// <summary>مفتاح السجل المتأثّر.</summary>
    public string? EntityId { get; set; }

    /// <summary>نوع العملية: Create / Update / Delete / Login ...</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>القيمة قبل التغيير (JSON).</summary>
    public string? Before { get; set; }

    /// <summary>القيمة بعد التغيير (JSON).</summary>
    public string? After { get; set; }

    // ── توثيق تعديل سجل التدقيق نفسه (FR-AUD-01) ──
    public string? EditedBy { get; set; }
    public DateTime? EditedAt { get; set; }
    /// <summary>سبب التعديل عند تحرير السجل بصلاحية Audit.Edit.</summary>
    public string? EditReason { get; set; }
}
