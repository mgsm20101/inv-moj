using WIMS.Domain.Catalog;
using WIMS.Domain.Common;
using WIMS.Domain.Enums;

namespace WIMS.Domain.Custody;

/// <summary>بند عهدة — وحدة عهدة واحدة (سيريال واحد للمتتبَّع بسيريال). مشتقّ من حركة دفترية.</summary>
public class CustodyItem : BaseEntity
{
    public Guid CustodyId { get; set; }
    public Custody Custody { get; set; } = null!;

    public Guid ItemId { get; set; }
    public Item Item { get; set; } = null!;

    public string? SerialNo { get; set; }
    public decimal Qty { get; set; } = 1;

    /// <summary>الرابط بالدفتر — من أي حركة صرف نشأت العهدة (BR-CUS-04).</summary>
    public Guid SourceStockTransactionId { get; set; }
    public Guid SourceVoucherId { get; set; }

    public CustodyItemStatus Status { get; set; } = CustodyItemStatus.InCustody;
    public DateTime AssignedAt { get; set; }
    public DateTime? ReleasedAt { get; set; }

    public decimal UnitCost { get; set; }
    public string? ConditionNote { get; set; }
}
