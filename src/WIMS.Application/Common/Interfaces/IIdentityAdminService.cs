using WIMS.Shared.Results;

namespace WIMS.Application.Common.Interfaces;

/// <summary>
/// تجريد إدارة المستخدمين والأدوار وإسناد الصلاحيات (ASP.NET Core Identity) لطبقة Application.
/// يُنفَّذ في Infrastructure عبر UserManager/RoleManager + AppDbContext. تُستدعى من معالجات CQRS
/// (Features/Admin) لتبقى مُدقَّقة عبر AuditBehavior ومحكومة بالتحقق.
/// </summary>
public interface IIdentityAdminService
{
    // ── المستخدمون ──
    Task<IReadOnlyList<UserSummaryDto>> GetUsersAsync(CancellationToken ct);
    Task<Result<UserDetailDto>> GetUserAsync(Guid id, CancellationToken ct);
    Task<Result<Guid>> CreateUserAsync(
        string userName, string fullName, string? email, string password,
        IReadOnlyList<Guid> roleIds, CancellationToken ct);
    Task<Result> UpdateUserAsync(Guid id, string fullName, string? email, CancellationToken ct);
    Task<Result> SetUserActiveAsync(Guid id, bool isActive, CancellationToken ct);
    Task<Result> ResetPasswordAsync(Guid id, string newPassword, CancellationToken ct);
    Task<Result> SetUserRolesAsync(Guid id, IReadOnlyList<Guid> roleIds, CancellationToken ct);
    Task<Result> ChangeOwnPasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken ct);

    // ── الأدوار ──
    Task<IReadOnlyList<RoleSummaryDto>> GetRolesAsync(CancellationToken ct);
    Task<Result<RoleDetailDto>> GetRoleAsync(Guid id, CancellationToken ct);
    Task<Result<Guid>> CreateRoleAsync(
        string name, string? description, IReadOnlyList<string> permissionKeys, CancellationToken ct);
    Task<Result> UpdateRoleAsync(Guid id, string name, string? description, CancellationToken ct);
    Task<Result> SetRolePermissionsAsync(Guid id, IReadOnlyList<string> permissionKeys, CancellationToken ct);
    Task<Result> DeleteRoleAsync(Guid id, CancellationToken ct);
}

// ─────────────────────── نماذج العرض (DTOs) ───────────────────────

public sealed record UserSummaryDto(
    Guid Id, string UserName, string FullName, string? Email, bool IsActive, IReadOnlyList<string> Roles);

public sealed record UserDetailDto(
    Guid Id, string UserName, string FullName, string? Email, bool IsActive,
    IReadOnlyList<Guid> RoleIds, IReadOnlyList<string> Roles);

public sealed record RoleSummaryDto(
    Guid Id, string Name, string? Description, int PermissionCount, int UserCount, bool IsSystem);

public sealed record RoleDetailDto(
    Guid Id, string Name, string? Description, bool IsSystem, IReadOnlyList<string> PermissionKeys);
