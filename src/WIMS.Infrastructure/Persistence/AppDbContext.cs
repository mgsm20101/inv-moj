using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WIMS.Application.Common.Interfaces;
using WIMS.Domain.Alerts;
using WIMS.Domain.Auditing;
using WIMS.Domain.Authorization;
using WIMS.Domain.Approvals;
using WIMS.Domain.Catalog;
using WIMS.Domain.Common;
using WIMS.Domain.Custody;
using WIMS.Domain.Employees;
using WIMS.Domain.Inventory;
using WIMS.Domain.Suppliers;
using WIMS.Domain.Transactions;
using WIMS.Domain.Warehousing;
using WIMS.Infrastructure.Identity;

namespace WIMS.Infrastructure.Persistence;

/// <summary>
/// سياق قاعدة البيانات: يدمج جداول ASP.NET Identity مع كيانات النطاق،
/// يملأ حقول التدقيق آلياً، ويطبّق الحذف الناعم (Soft Delete) عبر Query Filter عام.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUser currentUser)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options), IAppDbContext
{
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<ItemCategory> ItemCategories => Set<ItemCategory>();
    public DbSet<UnitOfMeasure> UnitsOfMeasure => Set<UnitOfMeasure>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<WarehouseLocation> WarehouseLocations => Set<WarehouseLocation>();
    public DbSet<StockBalance> StockBalances => Set<StockBalance>();

    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Voucher> Vouchers => Set<Voucher>();
    public DbSet<VoucherLine> VoucherLines => Set<VoucherLine>();
    public DbSet<StockTransaction> StockTransactions => Set<StockTransaction>();

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Domain.Custody.Custody> Custodies => Set<Domain.Custody.Custody>();
    public DbSet<CustodyItem> CustodyItems => Set<CustodyItem>();
    public DbSet<ApprovalWorkflow> ApprovalWorkflows => Set<ApprovalWorkflow>();
    public DbSet<ApprovalStep> ApprovalSteps => Set<ApprovalStep>();
    public DbSet<ApprovalRequest> ApprovalRequests => Set<ApprovalRequest>();
    public DbSet<ApprovalAction> ApprovalActions => Set<ApprovalAction>();

    public DbSet<StockCount> StockCounts => Set<StockCount>();
    public DbSet<StockCountLine> StockCountLines => Set<StockCountLine>();
    public DbSet<Alert> Alerts => Set<Alert>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // تسلسل الدفتر (StockTransactions.TransactionNo).
        builder.HasSequence<long>("StockTxnSeq").StartsAt(1).IncrementsBy(1);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditInformation();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>يملأ CreatedAt/By وModifiedAt/By ويحوّل الحذف إلى حذف ناعم.</summary>
    private void ApplyAuditInformation()
    {
        var now = DateTime.UtcNow;
        var user = currentUser.UserName ?? "system";

        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = user;
                    break;

                case EntityState.Modified:
                    entry.Entity.ModifiedAt = now;
                    entry.Entity.ModifiedBy = user;
                    break;

                case EntityState.Deleted when entry.Entity is BaseEntity softDeletable:
                    // حذف ناعم بدل الحذف الفعلي حفاظاً على التدقيق.
                    entry.State = EntityState.Modified;
                    softDeletable.IsDeleted = true;
                    entry.Entity.ModifiedAt = now;
                    entry.Entity.ModifiedBy = user;
                    break;
            }
        }
    }
}
