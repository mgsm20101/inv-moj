using Microsoft.AspNetCore.Authorization;
using WIMS.Infrastructure.Identity;

namespace WIMS.WebApi.Authorization;

/// <summary>يمنح الوصول إذا حمل المستخدم Claim الصلاحية المطلوبة.</summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.HasClaim(JwtTokenService.PermissionClaimType, requirement.Permission))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
