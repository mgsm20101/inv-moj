using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WIMS.Domain.Authorization;
using WIMS.Infrastructure.Identity;

namespace WIMS.Infrastructure.Persistence;

/// <summary>
/// يهيّئ البيانات الأساسية: الصلاحيات، الأدوار الدقيقة (مدير النظام/أمين مخزن/معتمِد/مالي/مدقّق)،
/// مستخدم Admin افتراضي، ومسارات الموافقة الموجّهة حسب الدور.
/// يُستدعى عند الإقلاع (Program) وهو Idempotent (آمن للتكرار).
/// </summary>
public static class DbSeeder
{
    public const string AdminRoleName = SystemRoles.Admin;

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var db = sp.GetRequiredService<AppDbContext>();
        var roleManager = sp.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var config = sp.GetRequiredService<IConfiguration>();
        var logger = sp.GetRequiredService<ILogger<AppDbContextSeedMarker>>();

        await db.Database.MigrateAsync(cancellationToken);

        await SeedPermissionsAsync(db, cancellationToken);

        // ── الأدوار: مدير النظام (كل الصلاحيات) + أدوار دقيقة بمجموعات صلاحيات ──
        var adminRole = await SeedAdminRoleAsync(db, roleManager, cancellationToken);
        var keeperRole = await SeedRoleAsync(db, roleManager, SystemRoles.WarehouseKeeper,
            "إدارة الأصناف والحركات والأرصدة والجرد والاستيراد.", WarehouseKeeperPermissions(), cancellationToken);
        var approverRole = await SeedRoleAsync(db, roleManager, SystemRoles.Approver,
            "اعتماد الحركات (الخطوة الأولى).", ApprovalPermissions(), cancellationToken);
        var financeRole = await SeedRoleAsync(db, roleManager, SystemRoles.Finance,
            "الاعتماد المالي (الخطوة الثانية للصرف الكبير).", ApprovalPermissions(), cancellationToken);
        await SeedRoleAsync(db, roleManager, SystemRoles.Auditor,
            "عرض القراءة والتقارير وسجل التدقيق.", AuditorPermissions(), cancellationToken);

        // ── المستخدمون ──
        var adminPassword = config["Seed:AdminPassword"] is { Length: > 0 } p ? p : "Admin@12345";
        await SeedUserAsync(userManager, adminRole, "admin", "مدير النظام", adminPassword, "admin@moj.local", logger, cancellationToken);

        // حسابات تجربة لفصل الواجبات — تُعطَّل في بيئة التسليم عبر Seed:CreateDemoUsers=false.
        if (config.GetValue("Seed:CreateDemoUsers", true))
        {
            await SeedUserAsync(userManager, approverRole, "approver", "المعتمد", "Approver@12345", "approver@moj.local", logger, cancellationToken);
            await SeedUserAsync(userManager, financeRole, "finance", "المدير المالي", "Finance@12345", "finance@moj.local", logger, cancellationToken);
        }

        await SeedApprovalWorkflowsAsync(db, cancellationToken);
    }

    // ─────────────────────── مجموعات صلاحيات الأدوار ───────────────────────

    private static IReadOnlyList<string> WarehouseKeeperPermissions() =>
    [
        PermissionKeys.Items.View, PermissionKeys.Items.Manage,
        PermissionKeys.Warehouses.View, PermissionKeys.Suppliers.View, PermissionKeys.Employees.View,
        PermissionKeys.Vouchers.View, PermissionKeys.Vouchers.Create, PermissionKeys.Vouchers.Submit, PermissionKeys.Vouchers.Cancel,
        PermissionKeys.Custody.View, PermissionKeys.StockCount.View, PermissionKeys.StockCount.Manage,
        PermissionKeys.Alerts.View, PermissionKeys.Import.Execute,
        PermissionKeys.Reports.View, PermissionKeys.Dashboard.View,
    ];

    private static IReadOnlyList<string> ApprovalPermissions() =>
    [
        PermissionKeys.Approvals.View, PermissionKeys.Approvals.Act,
        PermissionKeys.Vouchers.View, PermissionKeys.Vouchers.Approve,
        PermissionKeys.Reports.View, PermissionKeys.Dashboard.View,
    ];

    private static IReadOnlyList<string> AuditorPermissions() =>
        PermissionKeys.All.Where(k => k.Key.EndsWith(".View", StringComparison.Ordinal))
            .Select(k => k.Key).ToList();

    // ─────────────────────── مسارات الموافقة (موجّهة حسب الدور) ───────────────────────

    private static async Task SeedApprovalWorkflowsAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.ApprovalWorkflows.AnyAsync(ct))
            return;

        // صرف صغير (< 5000): خطوة واحدة (معتمِد).
        db.ApprovalWorkflows.Add(new Domain.Approvals.ApprovalWorkflow
        {
            Name = "صرف صغير", TargetType = Domain.Enums.ApprovalTargetType.Voucher,
            VoucherType = Domain.Enums.VoucherType.Issue, MinAmount = null, MaxAmount = 5000, IsActive = true,
            Steps = { new Domain.Approvals.ApprovalStep { StepOrder = 1, Name = "اعتماد المعتمِد", ApproverRole = SystemRoles.Approver } },
        });

        // صرف كبير (≥ 5000): خطوتان (معتمِد ثم مدير مالي).
        db.ApprovalWorkflows.Add(new Domain.Approvals.ApprovalWorkflow
        {
            Name = "صرف كبير", TargetType = Domain.Enums.ApprovalTargetType.Voucher,
            VoucherType = Domain.Enums.VoucherType.Issue, MinAmount = 5000, MaxAmount = null, IsActive = true,
            Steps =
            {
                new Domain.Approvals.ApprovalStep { StepOrder = 1, Name = "اعتماد المعتمِد", ApproverRole = SystemRoles.Approver },
                new Domain.Approvals.ApprovalStep { StepOrder = 2, Name = "اعتماد المدير المالي", ApproverRole = SystemRoles.Finance },
            },
        });

        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedPermissionsAsync(AppDbContext db, CancellationToken ct)
    {
        var existing = await db.Permissions.Select(p => p.Key).ToListAsync(ct);
        foreach (var (key, name, module) in PermissionKeys.All)
        {
            if (existing.Contains(key)) continue;
            db.Permissions.Add(new Permission { Key = key, Name = name, Module = module });
        }
        await db.SaveChangesAsync(ct);
    }

    private static async Task<ApplicationRole> SeedAdminRoleAsync(
        AppDbContext db, RoleManager<ApplicationRole> roleManager, CancellationToken ct)
    {
        var role = await roleManager.FindByNameAsync(AdminRoleName);
        if (role is null)
        {
            role = new ApplicationRole(AdminRoleName) { Description = "دور بكامل الصلاحيات" };
            await roleManager.CreateAsync(role);
        }

        // منح الدور كل الصلاحيات غير الممنوحة بعد.
        var allPermissionIds = await db.Permissions.Select(p => p.Id).ToListAsync(ct);
        var grantedIds = await db.RolePermissions
            .Where(rp => rp.RoleId == role.Id)
            .Select(rp => rp.PermissionId)
            .ToListAsync(ct);

        foreach (var permissionId in allPermissionIds.Except(grantedIds))
            db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permissionId });

        await db.SaveChangesAsync(ct);
        return role;
    }

    /// <summary>ينشئ دوراً بمجموعة صلاحيات (Idempotent): يُنشئ الدور إن غاب ويمنح الصلاحيات غير الممنوحة.</summary>
    private static async Task<ApplicationRole> SeedRoleAsync(
        AppDbContext db, RoleManager<ApplicationRole> roleManager,
        string name, string description, IReadOnlyList<string> permissionKeys, CancellationToken ct)
    {
        var role = await roleManager.FindByNameAsync(name);
        if (role is null)
        {
            role = new ApplicationRole(name) { Description = description };
            await roleManager.CreateAsync(role);
        }

        var permissionIds = await db.Permissions
            .Where(p => permissionKeys.Contains(p.Key))
            .Select(p => p.Id).ToListAsync(ct);
        var grantedIds = await db.RolePermissions
            .Where(rp => rp.RoleId == role.Id)
            .Select(rp => rp.PermissionId).ToListAsync(ct);

        foreach (var permissionId in permissionIds.Except(grantedIds))
            db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permissionId });

        await db.SaveChangesAsync(ct);
        return role;
    }

    /// <summary>ينشئ مستخدماً بدور محدّد إن لم يوجد.</summary>
    private static async Task SeedUserAsync(
        UserManager<ApplicationUser> userManager, ApplicationRole role,
        string userName, string fullName, string password, string email, ILogger logger, CancellationToken ct)
    {
        if (await userManager.FindByNameAsync(userName) is not null)
            return;

        var user = new ApplicationUser
        {
            UserName = userName,
            Email = email,
            EmailConfirmed = true,
            FullName = fullName,
            IsActive = true,
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            logger.LogError("فشل إنشاء المستخدم {User}: {Errors}", userName,
                string.Join(", ", result.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(user, role.Name!);
        logger.LogWarning("تم إنشاء مستخدم ({User}) بدور ({Role}) — يجب تغيير كلمة المرور فوراً.", userName, role.Name);
    }

    /// <summary>نوع علامة (Marker) للحصول على Logger مُصنَّف.</summary>
    public sealed class AppDbContextSeedMarker;
}
