using Microsoft.EntityFrameworkCore;
using WIMS.Domain.Alerts;
using WIMS.Domain.Auditing;
using WIMS.Domain.Authorization;
using WIMS.Domain.Approvals;
using WIMS.Domain.Catalog;
using WIMS.Domain.Custody;
using WIMS.Domain.Employees;
using WIMS.Domain.Inventory;
using WIMS.Domain.Suppliers;
using WIMS.Domain.Transactions;
using WIMS.Domain.Warehousing;

namespace WIMS.Application.Common.Interfaces;

/// <summary>
/// تجريد سياق قاعدة البيانات المتاح لطبقة Application (بدون معرفة موفّر SQL Server).
/// يُنفَّذ في Infrastructure عبر AppDbContext.
/// </summary>
public interface IAppDbContext
{
    DbSet<Permission> Permissions { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<AuditLog> AuditLogs { get; }

    // ── المرحلة 1: الكتالوج والمخازن والأرصدة ──
    DbSet<ItemCategory> ItemCategories { get; }
    DbSet<UnitOfMeasure> UnitsOfMeasure { get; }
    DbSet<Item> Items { get; }
    DbSet<Warehouse> Warehouses { get; }
    DbSet<WarehouseLocation> WarehouseLocations { get; }
    DbSet<StockBalance> StockBalances { get; }

    // ── المرحلة 2: الحركات ──
    DbSet<Supplier> Suppliers { get; }
    DbSet<Voucher> Vouchers { get; }
    DbSet<VoucherLine> VoucherLines { get; }
    DbSet<StockTransaction> StockTransactions { get; }

    // ── المرحلة 3: العُهد والموافقات ──
    DbSet<Employee> Employees { get; }
    DbSet<Domain.Custody.Custody> Custodies { get; }
    DbSet<CustodyItem> CustodyItems { get; }
    DbSet<ApprovalWorkflow> ApprovalWorkflows { get; }
    DbSet<ApprovalStep> ApprovalSteps { get; }
    DbSet<ApprovalRequest> ApprovalRequests { get; }
    DbSet<ApprovalAction> ApprovalActions { get; }

    // ── المرحلة 4: الجرد والإنذارات ──
    DbSet<StockCount> StockCounts { get; }
    DbSet<StockCountLine> StockCountLines { get; }
    DbSet<Alert> Alerts { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
