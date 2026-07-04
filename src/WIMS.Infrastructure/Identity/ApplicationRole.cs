using Microsoft.AspNetCore.Identity;

namespace WIMS.Infrastructure.Identity;

/// <summary>دور النظام — يُربط بالصلاحيات الدقيقة عبر RolePermission.</summary>
public class ApplicationRole : IdentityRole<Guid>
{
    public ApplicationRole() { }
    public ApplicationRole(string roleName) : base(roleName) { }

    public string? Description { get; set; }
}
