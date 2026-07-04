using WIMS.Domain.Catalog;
using WIMS.Domain.Common;
using WIMS.Domain.Enums;
using WIMS.Domain.Warehousing;

namespace WIMS.Domain.Alerts;

/// <summary>
/// إنذار مخزني مُكتشَف آلياً (نقطة إعادة الطلب/الحد الأدنى/قرب الصلاحية/الركود).
/// يُخزَّن كسجل داخل النظام ويُرسَل بريدياً للحرِج. مفتاح DedupKey يمنع التكرار (FR-REO-01..05).
/// </summary>
public class Alert : BaseEntity
{
    public AlertType AlertType { get; set; }
    public AlertSeverity Severity { get; set; }
    public AlertStatus Status { get; set; } = AlertStatus.Open;

    public Guid ItemId { get; set; }
    public Item Item { get; set; } = null!;

    public Guid? WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }

    public string? BatchNo { get; set; }

    /// <summary>الرسالة العربية المعروضة للمستخدم.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>القيمة الملحوظة (الرصيد الحالي/الأيام المتبقّية).</summary>
    public decimal? ObservedValue { get; set; }

    /// <summary>الحد الذي أطلق الإنذار (نقطة الطلب/الحد الأدنى/عتبة الأيام).</summary>
    public decimal? ThresholdValue { get; set; }

    /// <summary>مفتاح إزالة التكرار: النوع|الصنف|المخزن|الدُفعة — يمنع تكرار الإنذار المفتوح.</summary>
    public string DedupKey { get; set; } = string.Empty;

    public DateTime DetectedAt { get; set; }

    public string? AcknowledgedBy { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }

    /// <summary>هل أُرسِل إشعار بريدي لهذا الإنذار.</summary>
    public bool EmailSent { get; set; }
}
