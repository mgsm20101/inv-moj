using WIMS.Domain.Common;
using WIMS.Domain.Enums;

namespace WIMS.Domain.Warehousing;

/// <summary>موقع تخزين داخل مخزن (ممر-رف-صندوق). النوع يُطابَق مع نوع الصنف (BR-05).</summary>
public class WarehouseLocation : BaseEntity
{
    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;

    /// <summary>كود الموقع "A-01-03" (فريد داخل المخزن).</summary>
    public string Code { get; set; } = string.Empty;

    public string? Zone { get; set; }
    public string? Rack { get; set; }
    public string? Bin { get; set; }

    public LocationType LocationType { get; set; } = LocationType.Standard;

    public bool IsActive { get; set; } = true;
}
