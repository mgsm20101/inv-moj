using WIMS.Domain.Common;

namespace WIMS.Domain.Authorization;

/// <summary>
/// صلاحية دقيقة (Fine-grained) مثل "Items.Create". تُربط بالأدوار عبر <see cref="RolePermission"/>
/// وتُترجم إلى Authorization Policies في الطبقة الخارجية (RBAC).
/// </summary>
public class Permission : BaseEntity
{
    /// <summary>المفتاح الفريد للصلاحية، مثل "Items.Create" — يطابق اسم الـ Policy.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>الاسم المعروض بالعربية.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>الوحدة/الموديول الذي تنتمي إليه الصلاحية (أصناف، مخازن، تدقيق...).</summary>
    public string Module { get; set; } = string.Empty;

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
