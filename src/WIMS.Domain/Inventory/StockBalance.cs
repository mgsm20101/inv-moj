using WIMS.Domain.Catalog;
using WIMS.Domain.Common;
using WIMS.Domain.Warehousing;

namespace WIMS.Domain.Inventory;

/// <summary>
/// الرصيد الحيّ على مستوى (صنف × مخزن × موقع × دُفعة/سيريال).
/// يحمل RowVersion كـ Concurrency Token لمنع الصرف المزدوج (BR-11).
/// </summary>
public class StockBalance : BaseEntity
{
    public Guid ItemId { get; set; }
    public Item Item { get; set; } = null!;

    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;

    public Guid? LocationId { get; set; }
    public WarehouseLocation? Location { get; set; }

    /// <summary>رقم الدُفعة — إلزامي إذا Item.TracksBatch.</summary>
    public string? BatchNo { get; set; }

    /// <summary>رقم السيريال — إلزامي إذا Item.TracksSerial (والكمية = 1).</summary>
    public string? SerialNo { get; set; }

    /// <summary>تاريخ الصلاحية — إلزامي إذا Item.TracksExpiry.</summary>
    public DateOnly? ExpiryDate { get; set; }

    /// <summary>الرصيد الفعلي بوحدة الأساس (BR-02: ≥ 0).</summary>
    public decimal QtyOnHand { get; set; }

    /// <summary>المحجوز لطلب صرف قيد التنفيذ.</summary>
    public decimal QtyReserved { get; set; }

    /// <summary>المتاح = QtyOnHand − QtyReserved (عمود محسوب في قاعدة البيانات).</summary>
    public decimal QtyAvailable { get; private set; }

    /// <summary>تكلفة هذه الدُفعة تحديداً.</summary>
    public decimal AvgCost { get; set; }

    /// <summary>Concurrency Token (rowversion) — يمنع التعارض عند التحديث المتزامن (BR-11).</summary>
    public byte[] RowVersion { get; set; } = [];
}
