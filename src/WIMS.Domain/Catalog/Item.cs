using WIMS.Domain.Common;
using WIMS.Domain.Enums;
using WIMS.Domain.Inventory;

namespace WIMS.Domain.Catalog;

/// <summary>الصنف — قلب الكتالوج. يحمل بيانات التتبّع والحدود والتكلفة (BR-01, BR-04, BR-07, BR-10).</summary>
public class Item : BaseEntity
{
    /// <summary>الكود الموحّد الفريد "GG-CCCC-SSSS" (BR-01).</summary>
    public string ItemCode { get; set; } = string.Empty;

    /// <summary>باركود المورّد/GS1 إن وُجد (فريد عند وجوده).</summary>
    public string? Barcode { get; set; }

    public string NameAr { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public string? Description { get; set; }

    public Guid CategoryId { get; set; }
    public ItemCategory Category { get; set; } = null!;

    public ItemType ItemType { get; set; } = ItemType.Consumable;

    public Guid BaseUnitId { get; set; }
    public UnitOfMeasure BaseUnit { get; set; } = null!;

    // ── متابعة المخزون (BR-04) ──
    public bool TracksBatch { get; set; }
    public bool TracksExpiry { get; set; }
    public bool TracksSerial { get; set; }

    // ── حدود المخزون (BR-07): MinStock ≤ ReorderPoint ≤ MaxStock ──
    public decimal MinStock { get; set; }
    public decimal? MaxStock { get; set; }
    public decimal ReorderPoint { get; set; }
    public decimal? ReorderQty { get; set; }

    // ── التكلفة (BR-10) ──
    public decimal WeightedAvgCost { get; set; }
    public decimal? LastPurchaseCost { get; set; }

    // ── بيانات الصنف الخطر / القابل للتلف (BR-05, BR-06) ──
    public string? HazardClass { get; set; }
    public string? StorageConditions { get; set; }
    public int? ShelfLifeDays { get; set; }

    // ── الحالة (BR-08) ──
    public bool IsActive { get; set; } = true;
    public bool IsStockItem { get; set; } = true;

    /// <summary>هل يُدار كعهدة عند صرفه (المرحلة 3). افتراضياً true للمستديم.</summary>
    public bool RequiresCustody { get; set; }

    public ICollection<StockBalance> StockBalances { get; set; } = new List<StockBalance>();
}
