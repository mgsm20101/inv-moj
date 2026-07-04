using WIMS.Domain.Common;
using WIMS.Domain.Employees;
using WIMS.Domain.Enums;

namespace WIMS.Domain.Custody;

/// <summary>ملف عهدة لحائز واحد (ملف نشط واحد لكل موظف). يحوي بنوداً.</summary>
public class Custody : BaseEntity
{
    public string CustodyNo { get; set; } = string.Empty;
    public CustodyType CustodyType { get; set; } = CustodyType.Personal;

    /// <summary>الموظف الحائز — للعهدة الشخصية.</summary>
    public Guid? EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    /// <summary>المخزن — للعهدة المخزنية.</summary>
    public Guid? WarehouseId { get; set; }
    public Guid? KeeperUserId { get; set; }

    public CustodyStatus Status { get; set; } = CustodyStatus.Active;
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? Notes { get; set; }

    public ICollection<CustodyItem> Items { get; set; } = new List<CustodyItem>();
}
