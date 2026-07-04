using WIMS.Domain.Common;
using WIMS.Domain.Enums;

namespace WIMS.Domain.Employees;

/// <summary>الموظف — حائز العُهد الشخصية. قد يُربط بحساب مستخدم نظام.</summary>
public class Employee : BaseEntity
{
    public string EmployeeNo { get; set; } = string.Empty;
    public string NationalId { get; set; } = string.Empty;
    public string FullNameAr { get; set; } = string.Empty;
    public string? FullNameEn { get; set; }
    public string Department { get; set; } = string.Empty;
    public string? JobTitle { get; set; }
    public string CostCenter { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }

    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;

    /// <summary>ربط اختياري بحساب النظام (موظف واحد ↔ مستخدم واحد).</summary>
    public Guid? UserId { get; set; }

    public DateOnly? HireDate { get; set; }
    public DateOnly? TerminationDate { get; set; }
}
