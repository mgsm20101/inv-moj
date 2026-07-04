using WIMS.Domain.Common;
using WIMS.Domain.Enums;
using WIMS.Domain.Warehousing;

namespace WIMS.Domain.Inventory;

/// <summary>
/// محضر جرد — رأس عملية عدّ فعلي لمخزن. يلتقط الرصيد الدفتري لحظة التجميد،
/// يستقبل العدّ الفعلي، يحسب الفروقات، ثم يُرحِّل تسويات عند الاعتماد (FR-STK-01..06).
/// </summary>
public class StockCount : BaseEntity
{
    /// <summary>رقم المحضر "CNT-yyyy-NNNNNN" — يُولَّد عند التخطيط.</summary>
    public string CountNo { get; set; } = string.Empty;

    public StockCountType CountType { get; set; } = StockCountType.Full;
    public StockCountStatus Status { get; set; } = StockCountStatus.Draft;

    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;

    public string? ScopeNote { get; set; }

    // ── التجميد (Freeze) ──
    public DateTime? FrozenAt { get; set; }
    public string? FrozenBy { get; set; }

    // ── العدّ الفعلي ──
    public DateTime? CountedAt { get; set; }

    // ── الاعتماد (فصل الواجبات: المعتمِد ≠ من جمّد/أنشأ) ──
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }

    /// <summary>أرقام سندات التسوية المُولَّدة عند الاعتماد (مفصولة بفاصلة).</summary>
    public string? AdjustmentVoucherNos { get; set; }

    public string? Notes { get; set; }

    public ICollection<StockCountLine> Lines { get; set; } = new List<StockCountLine>();
}
