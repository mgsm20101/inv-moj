using WIMS.Domain.Common;
using WIMS.Domain.Enums;

namespace WIMS.Domain.Warehousing;

/// <summary>المخزن. الحالة (Status) تتحكّم بالسماح بالحركة (BR-03).</summary>
public class Warehouse : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;

    public WarehouseType WarehouseType { get; set; } = WarehouseType.Main;

    /// <summary>الفرع أو المنطقة الإدارية.</summary>
    public string? Region { get; set; }

    /// <summary>أمين المخزن المسؤول (مستخدم من المرحلة 0).</summary>
    public Guid KeeperUserId { get; set; }

    public WarehouseStatus Status { get; set; } = WarehouseStatus.Active;

    /// <summary>حارس صلب — يبقى false في الجهة الحكومية (BR-02).</summary>
    public bool AllowNegativeStock { get; set; }

    /// <summary>هل يستخدم المخزن مواقع (رفوف/صناديق) تفصيلية.</summary>
    public bool UsesLocations { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<WarehouseLocation> Locations { get; set; } = new List<WarehouseLocation>();
}
