using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WIMS.Application.Common.Interfaces;
using WIMS.Infrastructure.Persistence;
using WIMS.Shared.Results;

namespace WIMS.Infrastructure.Identity;

/// <summary>
/// تنفيذ المصادقة عبر ASP.NET Core Identity: التحقق من كلمة المرور، القفل بعد المحاولات الفاشلة،
/// وتجميع أدوار المستخدم وصلاحياته الدقيقة (RBAC) لبناء الرمز.
/// </summary>
public sealed class IdentityService(
    UserManager<ApplicationUser> userManager,
    AppDbContext dbContext)
    : IIdentityService
{
    public async Task<Result<AuthenticatedUser>> ValidateCredentialsAsync(
        string userName, string password, CancellationToken cancellationToken = default)
    {
        var invalid = Error.Unauthorized("Auth.InvalidCredentials", "اسم المستخدم أو كلمة المرور غير صحيحة.");

        var user = await userManager.FindByNameAsync(userName);
        if (user is null || !user.IsActive)
            return Result.Failure<AuthenticatedUser>(invalid);

        if (await userManager.IsLockedOutAsync(user))
            return Result.Failure<AuthenticatedUser>(
                Error.Unauthorized("Auth.LockedOut", "تم قفل الحساب مؤقتاً بسبب محاولات دخول فاشلة متكررة."));

        if (!await userManager.CheckPasswordAsync(user, password))
        {
            await userManager.AccessFailedAsync(user);
            return Result.Failure<AuthenticatedUser>(invalid);
        }

        await userManager.ResetAccessFailedCountAsync(user);

        var roleNames = await userManager.GetRolesAsync(user);
        var permissions = await GetPermissionsForRolesAsync(roleNames, cancellationToken);

        return new AuthenticatedUser(
            user.Id.ToString(),
            user.UserName!,
            roleNames.ToList(),
            permissions);
    }

    private async Task<IReadOnlyList<string>> GetPermissionsForRolesAsync(
        IEnumerable<string> roleNames, CancellationToken cancellationToken)
    {
        var names = roleNames.ToList();
        if (names.Count == 0)
            return [];

        // أسماء الأدوار → معرّفاتها → صلاحياتها الدقيقة من جدول RolePermissions.
        var roleIds = await dbContext.Roles
            .Where(r => r.Name != null && names.Contains(r.Name))
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        if (roleIds.Count == 0)
            return [];

        return await dbContext.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Select(rp => rp.Permission.Key)
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}
