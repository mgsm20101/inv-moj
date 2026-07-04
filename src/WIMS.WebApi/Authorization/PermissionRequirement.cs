using Microsoft.AspNetCore.Authorization;

namespace WIMS.WebApi.Authorization;

/// <summary>متطلّب صلاحية دقيقة — يتحقق من وجود Claim من نوع "permission" بالقيمة المطلوبة.</summary>
public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}
