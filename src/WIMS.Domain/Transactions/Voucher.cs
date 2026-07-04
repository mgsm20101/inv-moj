using WIMS.Domain.Common;
using WIMS.Domain.Enums;
using WIMS.Domain.Suppliers;
using WIMS.Domain.Warehousing;

namespace WIMS.Domain.Transactions;

/// <summary>
/// رأس المستند (السند/الإذن) — قرار تشغيلي واحد له دورة حياة واعتماد.
/// الرصيد لا يتغيّر إلا عند الاعتماد (Post).
/// </summary>
public class Voucher : BaseEntity
{
    /// <summary>رقم السند — يُولَّد عند الاعتماد (مسودة: DRAFT-xxxxxxxx).</summary>
    public string VoucherNo { get; set; } = string.Empty;

    public VoucherType VoucherType { get; set; }
    public VoucherStatus Status { get; set; } = VoucherStatus.Draft;

    /// <summary>المخزن المصدر (صرف/تحويل) أو الهدف (استلام).</summary>
    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;

    /// <summary>المخزن المستقبِل — للتحويل فقط.</summary>
    public Guid? ToWarehouseId { get; set; }
    public Warehouse? ToWarehouse { get; set; }

    /// <summary>المورّد — للاستلام فقط.</summary>
    public Guid? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    /// <summary>السند الأصلي — للمرتجع والحركة العكسية.</summary>
    public Guid? SourceVoucherId { get; set; }

    public string? ReferenceNo { get; set; }

    /// <summary>
    /// تاريخ الإذن المرجعي — يحدّده المستخدم لدعم إدخال البيانات التاريخية (بأثر رجعي).
    /// افتراضيًا تاريخ اليوم. لا يعيد ترتيب دفتر المخزون أو حساب WAC (يبقى ترتيب الإدخال).
    /// </summary>
    public DateOnly? DocumentDate { get; set; }

    public string? RequestingDept { get; set; }
    public string? Reason { get; set; }

    /// <summary>الموظف المستلِم — للصرف الذي يُنشئ عهدة مستديمة (المرحلة 3).</summary>
    public Guid? RecipientEmployeeId { get; set; }

    public AdjustmentType? AdjustmentType { get; set; }
    public TransferStatus? TransferStatus { get; set; }
    public bool IsReversed { get; set; }

    // ── فصل الواجبات (SoD) ──
    public string? SubmittedBy { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectedBy { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectReason { get; set; }
    public DateTime? PostedAt { get; set; }

    public string? Notes { get; set; }

    public ICollection<VoucherLine> Lines { get; set; } = new List<VoucherLine>();
}
