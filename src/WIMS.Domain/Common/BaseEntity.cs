namespace WIMS.Domain.Common;

/// <summary>
/// الكيان الأساسي: مفتاح Guid + حقول تدقيق (Auditing) + حذف ناعم (Soft Delete).
/// كل كيانات النطاق ترث منه ليُعبّأ التدقيق آلياً عبر AuditBehavior وSaveChanges.
/// </summary>
public abstract class BaseEntity : IAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }

    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }

    /// <summary>حذف ناعم — لا يُحذف السجل فعلياً حفاظاً على التدقيق (حسب معايير الجهة).</summary>
    public bool IsDeleted { get; set; }
}
