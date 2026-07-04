using Microsoft.EntityFrameworkCore;
using WIMS.Application.Common.Interfaces;
using WIMS.Domain.Alerts;
using WIMS.Domain.Enums;

namespace WIMS.Application.Features.Alerts;

/// <summary>إعدادات محرّك الإنذارات (عتبات الفحص).</summary>
public sealed class AlertScanOptions
{
    /// <summary>عدد الأيام قبل انتهاء الصلاحية لإطلاق إنذار قرب الانتهاء.</summary>
    public int NearExpiryDays { get; set; } = 30;

    /// <summary>عدد الأيام بلا حركة صرف لاعتبار الصنف راكداً.</summary>
    public int StagnantDays { get; set; } = 90;
}

/// <summary>ناتج جولة فحص واحدة.</summary>
public sealed record AlertScanResult(int Created, int Resolved, IReadOnlyList<Alert> CriticalNew);

public interface IAlertScanner
{
    /// <summary>يفحص كل قواعد الإنذار، ينشئ الجديد (مع إزالة التكرار) ويُغلق ما زال سببه.</summary>
    Task<AlertScanResult> ScanAsync(CancellationToken ct = default);
}

/// <summary>
/// محرّك اكتشاف الإنذارات (FR-REO-01..05): نقطة إعادة الطلب/الحد الأدنى،
/// قرب/انتهاء الصلاحية، والركود. يعتمد DedupKey لمنع تكرار الإنذار المفتوح
/// ويُغلق آلياً الإنذارات التي زال سببها.
/// </summary>
public sealed class AlertScanner(IAppDbContext db, AlertScanOptions options) : IAlertScanner
{
    public async Task<AlertScanResult> ScanAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);

        // الإنذارات المفتوحة حالياً (لإزالة التكرار والإغلاق التلقائي).
        var openAlerts = await db.Alerts.Where(a => a.Status != AlertStatus.Resolved).ToListAsync(ct);
        var openByKey = openAlerts.ToDictionary(a => a.DedupKey, a => a, StringComparer.OrdinalIgnoreCase);
        var stillActiveKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var created = new List<Alert>();

        void Raise(AlertType type, AlertSeverity sev, Guid itemId, Guid? whId, string? batch,
            string message, decimal? observed, decimal? threshold)
        {
            var key = $"{(byte)type}|{itemId}|{whId}|{batch}";
            stillActiveKeys.Add(key);
            if (openByKey.ContainsKey(key)) return; // مفتوح مسبقاً → لا تكرار.

            var alert = new Alert
            {
                AlertType = type, Severity = sev, Status = AlertStatus.Open,
                ItemId = itemId, WarehouseId = whId, BatchNo = batch,
                Message = message, ObservedValue = observed, ThresholdValue = threshold,
                DedupKey = key, DetectedAt = now,
            };
            db.Alerts.Add(alert);
            created.Add(alert);
        }

        // ── 1) نقطة إعادة الطلب / الحد الأدنى (رصيد إجمالي للصنف عبر كل المخازن) ──
        var items = await db.Items.AsNoTracking()
            .Where(i => i.IsActive && i.IsStockItem)
            .Select(i => new { i.Id, i.ItemCode, i.NameAr, i.MinStock, i.ReorderPoint })
            .ToListAsync(ct);

        var onHandByItem = await db.StockBalances.AsNoTracking()
            .GroupBy(b => b.ItemId)
            .Select(g => new { ItemId = g.Key, Qty = g.Sum(b => b.QtyOnHand) })
            .ToDictionaryAsync(x => x.ItemId, x => x.Qty, ct);

        foreach (var it in items)
        {
            var onHand = onHandByItem.GetValueOrDefault(it.Id, 0);
            if (it.MinStock > 0 && onHand <= it.MinStock)
                Raise(AlertType.MinStock, AlertSeverity.Critical, it.Id, null, null,
                    $"الصنف {it.ItemCode} ({it.NameAr}) بلغ/تحت الحد الأدنى: الرصيد {onHand} ≤ الحد {it.MinStock}.",
                    onHand, it.MinStock);
            else if (it.ReorderPoint > 0 && onHand <= it.ReorderPoint)
                Raise(AlertType.ReorderPoint, AlertSeverity.Warning, it.Id, null, null,
                    $"الصنف {it.ItemCode} ({it.NameAr}) بلغ نقطة إعادة الطلب: الرصيد {onHand} ≤ {it.ReorderPoint}.",
                    onHand, it.ReorderPoint);
        }

        // ── 2) قرب انتهاء الصلاحية / منتهي الصلاحية ──
        var limit = today.AddDays(options.NearExpiryDays);
        var expiring = await db.StockBalances.AsNoTracking()
            .Where(b => b.QtyOnHand > 0 && b.ExpiryDate != null && b.ExpiryDate <= limit)
            .Select(b => new { b.ItemId, b.WarehouseId, b.BatchNo, b.ExpiryDate, b.QtyOnHand,
                Code = b.Item.ItemCode, Name = b.Item.NameAr })
            .ToListAsync(ct);

        foreach (var b in expiring)
        {
            var daysLeft = b.ExpiryDate!.Value.DayNumber - today.DayNumber;
            if (daysLeft < 0)
                Raise(AlertType.Expired, AlertSeverity.Critical, b.ItemId, b.WarehouseId, b.BatchNo,
                    $"الصنف {b.Code} ({b.Name}) دُفعة {b.BatchNo} منتهي الصلاحية منذ {-daysLeft} يوم — الكمية {b.QtyOnHand}.",
                    daysLeft, 0);
            else
                Raise(AlertType.NearExpiry, AlertSeverity.Warning, b.ItemId, b.WarehouseId, b.BatchNo,
                    $"الصنف {b.Code} ({b.Name}) دُفعة {b.BatchNo} تنتهي صلاحيته خلال {daysLeft} يوم — الكمية {b.QtyOnHand}.",
                    daysLeft, options.NearExpiryDays);
        }

        // ── 3) الأصناف الراكدة (بلا حركة صرف منذ StagnantDays ولديها رصيد) ──
        var stagnantSince = now.AddDays(-options.StagnantDays);
        var outTypes = new[] { StockTxnType.Issue, StockTxnType.TransferOut };
        var lastIssueByItem = await db.StockTransactions.AsNoTracking()
            .Where(t => outTypes.Contains(t.TxnType))
            .GroupBy(t => t.ItemId)
            .Select(g => new { ItemId = g.Key, LastAt = g.Max(t => t.PostedAt) })
            .ToDictionaryAsync(x => x.ItemId, x => x.LastAt, ct);

        foreach (var it in items)
        {
            var onHand = onHandByItem.GetValueOrDefault(it.Id, 0);
            if (onHand <= 0) continue;
            var last = lastIssueByItem.GetValueOrDefault(it.Id);
            if (last == default || last < stagnantSince)
            {
                var sinceTxt = last == default ? "بلا أي حركة صرف مسجّلة" : $"آخر صرف {last:yyyy-MM-dd}";
                Raise(AlertType.Stagnant, AlertSeverity.Info, it.Id, null, null,
                    $"الصنف {it.ItemCode} ({it.NameAr}) راكد ({sinceTxt}) — الرصيد {onHand}.",
                    onHand, options.StagnantDays);
            }
        }

        // ── الإغلاق التلقائي: كل إنذار مفتوح لم يعُد سببه قائماً ──
        var resolved = 0;
        foreach (var a in openAlerts.Where(a => !stillActiveKeys.Contains(a.DedupKey)))
        {
            a.Status = AlertStatus.Resolved;
            a.ResolvedAt = now;
            resolved++;
        }

        if (created.Count > 0 || resolved > 0)
            await db.SaveChangesAsync(ct);

        var criticalNew = created.Where(a => a.Severity == AlertSeverity.Critical).ToList();
        return new AlertScanResult(created.Count, resolved, criticalNew);
    }
}
