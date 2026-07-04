using System.Security.Claims;
using WIMS.Application.Common.Interfaces;
using WIMS.Infrastructure.Identity;

namespace WIMS.WebApi.Services;

/// <summary>يقرأ هوية المستخدم الحالي وصلاحياته من HttpContext.</summary>
public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? User => accessor.HttpContext?.User;

    public string? UserId => User?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User?.FindFirstValue("sub");

    public string? UserName => User?.Identity?.Name
        ?? User?.FindFirstValue("unique_name");

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public bool HasPermission(string permissionKey)
        => User?.HasClaim(JwtTokenService.PermissionClaimType, permissionKey) ?? false;

    public bool IsInRole(string role) => User?.IsInRole(role) ?? false;
}
