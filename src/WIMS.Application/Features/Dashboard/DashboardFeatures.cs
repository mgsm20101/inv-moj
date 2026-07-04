using MediatR;
using Microsoft.EntityFrameworkCore;
using WIMS.Application.Common.Interfaces;
using WIMS.Application.Common.Messaging;
using WIMS.Domain.Enums;

namespace WIMS.Application.Features.Dashboard;

/// <summary>عنصر إنذار مختصر للوحة المؤشرات.</summary>
public sealed record DashboardAlertDto(
    Guid Id, AlertType AlertType, AlertSeverity Severity, string ItemName, string Message, DateTime DetectedAt);

/// <summary>صنف تحت الحد الأدنى (أعلى عجزاً).</summary>
public sealed record DashboardShortageDto(string ItemCode, string ItemName, decimal OnHand, decimal MinStock, decimal Shortage);

/// <summary>لوحة المؤشرات الرئيسية (FR-RPT-07).</summary>
public sealed record DashboardDto(
    int TotalItems,
    int ActiveItems,
    int TotalWarehouses,
    decimal TotalStockValue,
    int ItemsInStock,
    int BelowMinCount,
    int NearExpiryBatches,
    int ExpiredBatches,
    int PendingVouchers,
    int OpenStockCounts,
    int OpenAlerts,
    int CriticalAlerts,
    int WarningAlerts,
    IReadOnlyList<DashboardShortageDto> TopShortages,
    IReadOnlyList<DashboardAlertDto> RecentCriticalAlerts);

public sealed record GetDashboardQuery(int NearExpiryDays = 30) : IQuery<DashboardDto>;

public sealed class GetDashboardQueryHandler(IAppDbContext db)
    : IRequestHandler<GetDashboardQuery, DashboardDto>
{
    public async Task<DashboardDto> Handle(GetDashboardQuery request, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var nearExpiryLimit = today.AddDays(request.NearExpiryDays <= 0 ? 30 : request.NearExpiryDays);

        var totalItems = await db.Items.AsNoTracking().CountAsync(ct);
        var activeItems = await db.Items.AsNoTracking().CountAsync(i => i.IsActive, ct);
        var totalWarehouses = await db.Warehouses.AsNoTracking().CountAsync(w => w.IsActive, ct);

        var totalStockValue = await db.StockBalances.AsNoTracking()
            .Where(b => b.QtyOnHand > 0)
            .SumAsync(b => b.QtyOnHand * b.AvgCost, ct);

        var itemsInStock = await db.StockBalances.AsNoTracking()
            .Where(b => b.QtyOnHand > 0).Select(b => b.ItemId).Distinct().CountAsync(ct);

        // تحت الحد الأدنى — يُحسَب في الذاكرة من إجمالي الرصيد لكل صنف.
        var onHand = await db.StockBalances.AsNoTracking()
            .GroupBy(b => b.ItemId)
            .Select(g => new { ItemId = g.Key, Qty = g.Sum(x => x.QtyOnHand) })
            .ToDictionaryAsync(x => x.ItemId, x => x.Qty, ct);

        var minItems = await db.Items.AsNoTracking()
            .Where(i => i.IsActive && i.IsStockItem && i.MinStock > 0)
            .Select(i => new { i.ItemCode, i.NameAr, i.MinStock, ItemId = i.Id })
            .ToListAsync(ct);

        var shortages = minItems
            .Select(i => new { i.ItemCode, i.NameAr, i.MinStock, Qty = onHand.GetValueOrDefault(i.ItemId) })
            .Where(x => x.Qty < x.MinStock)
            .OrderByDescending(x => x.MinStock - x.Qty)
            .ToList();

        var topShortages = shortages.Take(5)
            .Select(x => new DashboardShortageDto(x.ItemCode, x.NameAr, x.Qty, x.MinStock, x.MinStock - x.Qty))
            .ToList();

        var nearExpiry = await db.StockBalances.AsNoTracking()
            .CountAsync(b => b.QtyOnHand > 0 && b.ExpiryDate != null
                          && b.ExpiryDate >= today && b.ExpiryDate <= nearExpiryLimit, ct);
        var expired = await db.StockBalances.AsNoTracking()
            .CountAsync(b => b.QtyOnHand > 0 && b.ExpiryDate != null && b.ExpiryDate < today, ct);

        var pendingVouchers = await db.Vouchers.AsNoTracking()
            .CountAsync(v => v.Status == VoucherStatus.UnderReview, ct);
        var openStockCounts = await db.StockCounts.AsNoTracking()
            .CountAsync(c => c.Status == StockCountStatus.Frozen || c.Status == StockCountStatus.UnderReview, ct);

        var openAlerts = await db.Alerts.AsNoTracking().CountAsync(a => a.Status == AlertStatus.Open, ct);
        var criticalAlerts = await db.Alerts.AsNoTracking()
            .CountAsync(a => a.Status == AlertStatus.Open && a.Severity == AlertSeverity.Critical, ct);
        var warningAlerts = await db.Alerts.AsNoTracking()
            .CountAsync(a => a.Status == AlertStatus.Open && a.Severity == AlertSeverity.Warning, ct);

        var recentCritical = await db.Alerts.AsNoTracking()
            .Where(a => a.Status == AlertStatus.Open && a.Severity == AlertSeverity.Critical)
            .OrderByDescending(a => a.DetectedAt)
            .Take(10)
            .Select(a => new DashboardAlertDto(a.Id, a.AlertType, a.Severity, a.Item.NameAr, a.Message, a.DetectedAt))
            .ToListAsync(ct);

        return new DashboardDto(
            totalItems, activeItems, totalWarehouses, totalStockValue, itemsInStock,
            shortages.Count, nearExpiry, expired, pendingVouchers, openStockCounts,
            openAlerts, criticalAlerts, warningAlerts, topShortages, recentCritical);
    }
}
