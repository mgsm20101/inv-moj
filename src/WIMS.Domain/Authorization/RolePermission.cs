namespace WIMS.Domain.Authorization;

/// <summary>
/// جدول ربط بين دور (ASP.NET Identity Role عبر <see cref="RoleId"/>) وصلاحية.
/// يمنح الدور مجموعة الصلاحيات الدقيقة.
/// </summary>
public class RolePermission
{
    public Guid RoleId { get; set; }

    public Guid PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
}
