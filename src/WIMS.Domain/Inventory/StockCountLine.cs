using WIMS.Domain.Catalog;
using WIMS.Domain.Common;

namespace WIMS.Domain.Inventory;

/// <summary>
/// سطر جرد لمفتاح مخزون واحد (صنف×موقع×دُفعة/سيريال). يحمل الرصيد الدفتري
/// المُلتقَط عند التجميد، والعدّ الفعلي، والفرق المحسوب.
/// </summary>
public class StockCountLine : BaseEntity
{
    public Guid StockCountId { get; set; }
    public StockCount StockCount { get; set; } = null!;

    public int LineNo { get; set; }

    public Guid ItemId { get; set; }
    public Item Item { get; set; } = null!;

    public Guid? LocationId { get; set; }
    public string? BatchNo { get; set; }
    public string? SerialNo { get; set; }
    public DateOnly? ExpiryDate { get; set; }

    /// <summary>الرصيد الدفتري لحظة التجميد (لقطة ثابتة).</summary>
    public decimal BookQty { get; set; }

    /// <summary>العدّ الفعلي — فارغ حتى يُدخَل.</summary>
    public decimal? PhysicalQty { get; set; }

    /// <summary>الفرق = الفعلي − الدفتري (موجب زيادة/عثور، سالب عجز/نقص).</summary>
    public decimal VarianceQty { get; set; }

    /// <summary>تكلفة الوحدة المُلتقَطة من الدُفعة عند التجميد.</summary>
    public decimal UnitCost { get; set; }

    /// <summary>قيمة الفرق = VarianceQty × UnitCost.</summary>
    public decimal VarianceValue { get; set; }

    public bool Counted { get; set; }
    public string? CountedBy { get; set; }
    public DateTime? CountedAt { get; set; }

    public string? Notes { get; set; }
}
