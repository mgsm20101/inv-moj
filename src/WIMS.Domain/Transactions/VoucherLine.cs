using WIMS.Domain.Catalog;
using WIMS.Domain.Common;

namespace WIMS.Domain.Transactions;

/// <summary>سطر السند — بند واحد. قابل للتعديل قبل الاعتماد فقط.</summary>
public class VoucherLine : BaseEntity
{
    public Guid VoucherId { get; set; }
    public Voucher Voucher { get; set; } = null!;

    public int LineNo { get; set; }

    public Guid ItemId { get; set; }
    public Item Item { get; set; } = null!;

    public Guid? LocationId { get; set; }
    public Guid? ToLocationId { get; set; }

    /// <summary>الكمية المطلوبة/المستلمة بوحدة الأساس (&gt; 0).</summary>
    public decimal Qty { get; set; }

    // ── GRN فقط: مطابق/مرفوض ──
    public decimal QtyAccepted { get; set; }
    public decimal QtyRejected { get; set; }
    public string? RejectReason { get; set; }

    // ── بيانات التتبّع حسب الصنف ──
    public string? BatchNo { get; set; }
    public string? SerialNo { get; set; }
    public DateOnly? ExpiryDate { get; set; }

    /// <summary>تكلفة الوحدة (استلام/تسوية زيادة: مُدخلة؛ صرف/صادر: تُملأ آلياً من تكلفة الدُفعة).</summary>
    public decimal UnitCost { get; set; }

    public string? Notes { get; set; }
}
