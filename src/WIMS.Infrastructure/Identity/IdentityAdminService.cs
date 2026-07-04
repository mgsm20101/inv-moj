using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WIMS.Application.Common.Interfaces;
using WIMS.Domain.Authorization;
using WIMS.Infrastructure.Persistence;
using WIMS.Shared.Results;

namespace WIMS.Infrastructure.Identity;

/// <summary>
/// إدارة المستخدمين والأدوار وإسناد الصلاحيات عبر ASP.NET Core Identity + جدول RolePermissions.
/// تفرض حراسات الأعمال: حماية دور مدير النظام، وعدم إسقاط آخر مدير فعّال.
/// </summary>
public sealed class IdentityAdminService(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    AppDbContext db)
    : IIdentityAdminService
{
    // ═══════════════════════ المستخدمون ═══════════════════════

    public async Task<IReadOnlyList<UserSummaryDto>> GetUsersAsync(CancellationToken ct)
    {
        // إسقاط صريح — لا نُحمّل بايتات الصورة في القائمة (HasPhoto فقط).
        var users = await userManager.Users.AsNoTracking().OrderBy(u => u.UserName)
            .Select(u => new
            {
                u.Id, u.UserName, u.FullName, u.Email, u.IsActive,
                HasPhoto = u.PhotoData != null,
            })
            .ToListAsync(ct);

        // أسماء الأدوار لكل مستخدم عبر ضمّة واحدة بدل N استعلامات.
        var roleMap = await (
            from ur in db.UserRoles
            join r in db.Roles on ur.RoleId equals r.Id
            where r.Name != null
            select new { ur.UserId, RoleName = r.Name! }).ToListAsync(ct);
        var rolesByUser = roleMap.GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.RoleName).ToList());

        return users.Select(u => new UserSummaryDto(
            u.Id, u.UserName!, u.FullName, u.Email, u.IsActive, u.HasPhoto,
            rolesByUser.TryGetValue(u.Id, out var rl) ? rl : [])).ToList();
    }

    public async Task<Result<UserDetailDto>> GetUserAsync(Guid id, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
            return Result.Failure<UserDetailDto>(Error.NotFound("User", "المستخدم غير موجود."));

        var roleNames = await userManager.GetRolesAsync(user);
        var roleIds = await db.Roles.AsNoTracking()
            .Where(r => r.Name != null && roleNames.Contains(r.Name))
            .Select(r => r.Id).ToListAsync(ct);

        return new UserDetailDto(user.Id, user.UserName!, user.FullName, user.Email, user.IsActive,
            user.PhotoData != null, roleIds, roleNames.ToList());
    }

    public async Task<Result<Guid>> CreateUserAsync(
        string userName, string fullName, string? email, string password,
        IReadOnlyList<Guid> roleIds, CancellationToken ct)
    {
        if (await userManager.FindByNameAsync(userName) is not null)
            return Result.Failure<Guid>(Error.Conflict("User.UserName", $"اسم المستخدم '{userName}' مستخدم مسبقاً."));

        var user = new ApplicationUser
        {
            UserName = userName,
            Email = string.IsNullOrWhiteSpace(email) ? null : email,
            EmailConfirmed = true,
            FullName = fullName,
            IsActive = true,
        };

        var create = await userManager.CreateAsync(user, password);
        if (!create.Succeeded)
            return Result.Failure<Guid>(ToError(create));

        var roleNames = await RoleNamesAsync(roleIds, ct);
        if (roleNames.Count > 0)
            await userManager.AddToRolesAsync(user, roleNames);

        return user.Id;
    }

    public async Task<Result> UpdateUserAsync(Guid id, string fullName, string? email, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null) return Result.Failure(Error.NotFound("User", "المستخدم غير موجود."));

        user.FullName = fullName;
        user.Email = string.IsNullOrWhiteSpace(email) ? null : email;
        var update = await userManager.UpdateAsync(user);
        return update.Succeeded ? Result.Success() : Result.Failure(ToError(update));
    }

    public async Task<Result> SetUserActiveAsync(Guid id, bool isActive, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null) return Result.Failure(Error.NotFound("User", "المستخدم غير موجود."));

        // حراسة: لا تُعطّل آخر مدير نظام فعّال.
        if (!isActive && await userManager.IsInRoleAsync(user, SystemRoles.Admin) && await IsLastActiveAdminAsync(user.Id, ct))
            return Result.Failure(Error.Conflict("User.LastAdmin", "لا يمكن تعطيل آخر مدير نظام فعّال."));

        user.IsActive = isActive;
        var update = await userManager.UpdateAsync(user);
        return update.Succeeded ? Result.Success() : Result.Failure(ToError(update));
    }

    public async Task<Result> ResetPasswordAsync(Guid id, string newPassword, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null) return Result.Failure(Error.NotFound("User", "المستخدم غير موجود."));

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var reset = await userManager.ResetPasswordAsync(user, token, newPassword);
        return reset.Succeeded ? Result.Success() : Result.Failure(ToError(reset));
    }

    public async Task<Result> SetUserRolesAsync(Guid id, IReadOnlyList<Guid> roleIds, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null) return Result.Failure(Error.NotFound("User", "المستخدم غير موجود."));

        var current = await userManager.GetRolesAsync(user);
        var target = await RoleNamesAsync(roleIds, ct);

        // حراسة: لا تُزل دور مدير النظام عن آخر مدير فعّال.
        if (current.Contains(SystemRoles.Admin) && !target.Contains(SystemRoles.Admin)
            && await IsLastActiveAdminAsync(user.Id, ct))
            return Result.Failure(Error.Conflict("User.LastAdmin", "لا يمكن إزالة دور مدير النظام عن آخر مدير فعّال."));

        var toRemove = current.Except(target).ToList();
        var toAdd = target.Except(current).ToList();

        if (toRemove.Count > 0)
        {
            var r = await userManager.RemoveFromRolesAsync(user, toRemove);
            if (!r.Succeeded) return Result.Failure(ToError(r));
        }
        if (toAdd.Count > 0)
        {
            var a = await userManager.AddToRolesAsync(user, toAdd);
            if (!a.Succeeded) return Result.Failure(ToError(a));
        }
        return Result.Success();
    }

    public async Task<Result> ChangeOwnPasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null) return Result.Failure(Error.NotFound("User", "المستخدم غير موجود."));

        var change = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!change.Succeeded)
        {
            // خطأ كلمة المرور الحالية يُعامل كتحقّق لا كخطأ خادم.
            return Result.Failure(Error.Validation("Password", string.Join(" ", change.Errors.Select(e => e.Description))));
        }
        return Result.Success();
    }

    // ═══════════════════════ صورة المستخدم ═══════════════════════

    public async Task<Result> SetUserPhotoAsync(Guid id, byte[] data, string contentType, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null) return Result.Failure(Error.NotFound("User", "المستخدم غير موجود."));

        user.PhotoData = data;
        user.PhotoContentType = contentType;
        var update = await userManager.UpdateAsync(user);
        return update.Succeeded ? Result.Success() : Result.Failure(ToError(update));
    }

    public async Task<UserPhotoDto?> GetUserPhotoAsync(Guid id, CancellationToken ct)
    {
        var photo = await db.Users.AsNoTracking().Where(u => u.Id == id)
            .Select(u => new { u.PhotoData, u.PhotoContentType })
            .FirstOrDefaultAsync(ct);

        return photo?.PhotoData is { Length: > 0 } data
            ? new UserPhotoDto(data, photo.PhotoContentType ?? "image/jpeg")
            : null;
    }

    public Task<bool> UserHasPhotoAsync(Guid id, CancellationToken ct)
        => db.Users.AsNoTracking().Where(u => u.Id == id)
            .Select(u => u.PhotoData != null).FirstOrDefaultAsync(ct);

    // ═══════════════════════ الأدوار ═══════════════════════

    public async Task<IReadOnlyList<RoleSummaryDto>> GetRolesAsync(CancellationToken ct)
    {
        var roles = await roleManager.Roles.AsNoTracking().OrderBy(r => r.Name).ToListAsync(ct);

        var permCounts = await db.RolePermissions.AsNoTracking()
            .GroupBy(rp => rp.RoleId).Select(g => new { g.Key, Count = g.Count() }).ToListAsync(ct);
        var userCounts = await db.UserRoles.AsNoTracking()
            .GroupBy(ur => ur.RoleId).Select(g => new { g.Key, Count = g.Count() }).ToListAsync(ct);

        return roles.Select(r => new RoleSummaryDto(
            r.Id, r.Name!, r.Description,
            permCounts.FirstOrDefault(p => p.Key == r.Id)?.Count ?? 0,
            userCounts.FirstOrDefault(u => u.Key == r.Id)?.Count ?? 0,
            r.Name == SystemRoles.Admin)).ToList();
    }

    public async Task<Result<RoleDetailDto>> GetRoleAsync(Guid id, CancellationToken ct)
    {
        var role = await roleManager.FindByIdAsync(id.ToString());
        if (role is null) return Result.Failure<RoleDetailDto>(Error.NotFound("Role", "الدور غير موجود."));

        var keys = await db.RolePermissions.AsNoTracking()
            .Where(rp => rp.RoleId == id).Select(rp => rp.Permission.Key).ToListAsync(ct);

        return new RoleDetailDto(role.Id, role.Name!, role.Description, role.Name == SystemRoles.Admin, keys);
    }

    public async Task<Result<Guid>> CreateRoleAsync(
        string name, string? description, IReadOnlyList<string> permissionKeys, CancellationToken ct)
    {
        if (await roleManager.RoleExistsAsync(name))
            return Result.Failure<Guid>(Error.Conflict("Role.Name", $"الدور '{name}' موجود مسبقاً."));

        var role = new ApplicationRole(name) { Description = description };
        var create = await roleManager.CreateAsync(role);
        if (!create.Succeeded) return Result.Failure<Guid>(ToError(create));

        var apply = await ReplaceRolePermissionsAsync(role.Id, permissionKeys, ct);
        if (apply.IsFailure) return Result.Failure<Guid>(apply.Error);

        return role.Id;
    }

    public async Task<Result> UpdateRoleAsync(Guid id, string name, string? description, CancellationToken ct)
    {
        var role = await roleManager.FindByIdAsync(id.ToString());
        if (role is null) return Result.Failure(Error.NotFound("Role", "الدور غير موجود."));

        if (role.Name == SystemRoles.Admin && name != SystemRoles.Admin)
            return Result.Failure(Error.Forbidden("Role.System", "لا يمكن إعادة تسمية دور مدير النظام."));

        if (!string.Equals(role.Name, name, StringComparison.Ordinal) && await roleManager.RoleExistsAsync(name))
            return Result.Failure(Error.Conflict("Role.Name", $"الدور '{name}' موجود مسبقاً."));

        role.Name = name;
        role.Description = description;
        var update = await roleManager.UpdateAsync(role);
        return update.Succeeded ? Result.Success() : Result.Failure(ToError(update));
    }

    public async Task<Result> SetRolePermissionsAsync(Guid id, IReadOnlyList<string> permissionKeys, CancellationToken ct)
    {
        var role = await roleManager.FindByIdAsync(id.ToString());
        if (role is null) return Result.Failure(Error.NotFound("Role", "الدور غير موجود."));

        // دور مدير النظام يجب أن يحتفظ بكل الصلاحيات — لا يُعدَّل.
        if (role.Name == SystemRoles.Admin)
            return Result.Failure(Error.Forbidden("Role.System", "صلاحيات دور مدير النظام ثابتة (كل الصلاحيات)."));

        return await ReplaceRolePermissionsAsync(id, permissionKeys, ct);
    }

    public async Task<Result> DeleteRoleAsync(Guid id, CancellationToken ct)
    {
        var role = await roleManager.FindByIdAsync(id.ToString());
        if (role is null) return Result.Failure(Error.NotFound("Role", "الدور غير موجود."));

        if (role.Name == SystemRoles.Admin)
            return Result.Failure(Error.Forbidden("Role.System", "لا يمكن حذف دور مدير النظام."));

        if (await db.UserRoles.AnyAsync(ur => ur.RoleId == id, ct))
            return Result.Failure(Error.Conflict("Role.InUse", "لا يمكن حذف دور مُسنَد إلى مستخدمين."));

        var existing = await db.RolePermissions.Where(rp => rp.RoleId == id).ToListAsync(ct);
        db.RolePermissions.RemoveRange(existing);
        await db.SaveChangesAsync(ct);

        var delete = await roleManager.DeleteAsync(role);
        return delete.Succeeded ? Result.Success() : Result.Failure(ToError(delete));
    }

    // ═══════════════════════ مساعدات ═══════════════════════

    private async Task<Result> ReplaceRolePermissionsAsync(Guid roleId, IReadOnlyList<string> keys, CancellationToken ct)
    {
        var distinctKeys = keys.Where(k => !string.IsNullOrWhiteSpace(k)).Distinct().ToList();

        var perms = await db.Permissions.Where(p => distinctKeys.Contains(p.Key))
            .Select(p => new { p.Id, p.Key }).ToListAsync(ct);

        if (perms.Count != distinctKeys.Count)
        {
            var unknown = distinctKeys.Except(perms.Select(p => p.Key));
            return Result.Failure(Error.Validation("Permissions", $"صلاحيات غير معروفة: {string.Join(", ", unknown)}."));
        }

        var existing = await db.RolePermissions.Where(rp => rp.RoleId == roleId).ToListAsync(ct);
        db.RolePermissions.RemoveRange(existing);
        foreach (var p in perms)
            db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = p.Id });

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    private async Task<List<string>> RoleNamesAsync(IReadOnlyList<Guid> roleIds, CancellationToken ct)
    {
        if (roleIds is null || roleIds.Count == 0) return [];
        return await db.Roles.Where(r => roleIds.Contains(r.Id) && r.Name != null)
            .Select(r => r.Name!).ToListAsync(ct);
    }

    /// <summary>هل هذا المستخدم هو المدير الفعّال الوحيد المتبقي؟</summary>
    private async Task<bool> IsLastActiveAdminAsync(Guid userId, CancellationToken ct)
    {
        var adminRoleId = await db.Roles.Where(r => r.Name == SystemRoles.Admin)
            .Select(r => r.Id).FirstOrDefaultAsync(ct);
        if (adminRoleId == Guid.Empty) return false;

        var activeAdminIds = await (
            from ur in db.UserRoles
            join u in db.Users on ur.UserId equals u.Id
            where ur.RoleId == adminRoleId && u.IsActive
            select ur.UserId).ToListAsync(ct);

        return activeAdminIds.Count <= 1 && activeAdminIds.Contains(userId);
    }

    private static Error ToError(IdentityResult result)
        => Error.Validation("Identity", string.Join(" ", result.Errors.Select(e => e.Description)));
}
