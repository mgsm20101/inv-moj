using WIMS.Domain.Catalog;
using WIMS.Domain.Common;
using WIMS.Domain.Enums;
using WIMS.Domain.Warehousing;

namespace WIMS.Domain.Transactions;

/// <summary>
/// الحركة الدفترية (Ledger) — سجل ذرّي غير قابل للتعديل يُنشأ لحظة الاعتماد.
/// سطر لكل تأثير على (صنف×مخزن×موقع×دُفعة/سيريال). التصحيح = حركة عكسية.
/// </summary>
public class StockTransaction : BaseEntity
{
    public Guid VoucherId { get; set; }
    public Voucher Voucher { get; set; } = null!;
    public Guid? VoucherLineId { get; set; }

    /// <summary>تسلسل عالمي متزايد (Sequence) — يعطي ترتيباً زمنياً قاطعاً للدفتر.</summary>
    public long TransactionNo { get; set; }

    public StockTxnType TxnType { get; set; }
    /// <summary>+1 إدخال، -1 إخراج.</summary>
    public short Direction { get; set; }

    public Guid ItemId { get; set; }
    public Item Item { get; set; } = null!;

    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;

    public Guid? LocationId { get; set; }
    public string? BatchNo { get; set; }
    public string? SerialNo { get; set; }
    public DateOnly? ExpiryDate { get; set; }

    /// <summary>دائماً موجب؛ الاتجاه من Direction.</summary>
    public decimal Qty { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }

    /// <summary>رصيد الدُفعة/الموقع بعد هذه الحركة (لقطة للمطابقة).</summary>
    public decimal QtyOnHandAfter { get; set; }
    /// <summary>WAC الصنف×المخزن بعد هذه الحركة.</summary>
    public decimal WacAfter { get; set; }

    public DateTime PostedAt { get; set; }
    public string PostedBy { get; set; } = string.Empty;

    /// <summary>يُملأ إذا كانت حركة عكسية لحركة سابقة.</summary>
    public Guid? ReversalOfTxnId { get; set; }
}
