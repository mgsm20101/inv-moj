using Microsoft.EntityFrameworkCore;
using WIMS.Application.Common.Interfaces;
using WIMS.Domain.Enums;
using WIMS.Domain.Inventory;
using WIMS.Domain.Transactions;
using WIMS.Shared.Results;

namespace WIMS.Application.Features.Transactions.Posting;

/// <summary>تنفيذ محرّك الترحيل — تكلفة طبقية بالدُفعة + FEFO/FIFO.</summary>
public sealed class VoucherPostingService(IAppDbContext db) : IVoucherPostingService
{
    public async Task<Result> PostAsync(Voucher voucher, string userName, CancellationToken cancellationToken = default)
    {
        // FR-STK-03: منع أي حركة على مخزن مُجمَّد بجرد جارٍ.
        var freeze = await EnsureNotFrozenAsync(voucher.WarehouseId, cancellationToken);
        if (freeze.IsFailure) return freeze;
        if (voucher.ToWarehouseId is not null)
        {
            var toFreeze = await EnsureNotFrozenAsync(voucher.ToWarehouseId.Value, cancellationToken);
            if (toFreeze.IsFailure) return toFreeze;
        }

        return voucher.VoucherType switch
        {
            VoucherType.Receipt => await PostReceiptAsync(voucher, userName, cancellationToken),
            VoucherType.Issue => await PostIssueLikeAsync(voucher, userName, StockTxnType.Issue, cancellationToken),
            VoucherType.Transfer => await PostTransferOutAsync(voucher, userName, cancellationToken),
            VoucherType.Return => await PostReturnAsync(voucher, userName, cancellationToken),
            VoucherType.Adjustment => await PostAdjustmentAsync(voucher, userName, cancellationToken),
            _ => Result.Failure(Error.Validation("Voucher.Type", "نوع سند غير مدعوم للترحيل.")),
        };
    }

    /// <summary>يمنع الترحيل إذا كان المخزن مُجمَّداً بجرد في حالة Frozen.</summary>
    private async Task<Result> EnsureNotFrozenAsync(Guid warehouseId, CancellationToken ct)
    {
        var frozen = await db.StockCounts
            .AnyAsync(c => c.WarehouseId == warehouseId && c.Status == StockCountStatus.Frozen, ct);
        return frozen
            ? Result.Failure(Error.Conflict("Stock.Frozen",
                "المخزن مُجمَّد لوجود جرد جارٍ — لا تُرحَّل حركات حتى اعتماد/إلغاء الجرد."))
            : Result.Success();
    }

    // ─────────────────────────── الاستلام (GRN) ───────────────────────────
    private async Task<Result> PostReceiptAsync(Voucher v, string user, CancellationToken ct)
    {
        var wh = await db.Warehouses.FirstOrDefaultAsync(w => w.Id == v.WarehouseId, ct);
        if (wh is null || wh.Status != WarehouseStatus.Active)
            return Result.Failure(Error.Validation("Warehouse", "لا يمكن الاستلام في مخزن مغلق/مجمّد."));

        foreach (var line in v.Lines.OrderBy(l => l.LineNo))
        {
            var accepted = line.QtyAccepted;
            if (accepted <= 0) continue;

            var balances = await LoadBalancesAsync(line.ItemId, v.WarehouseId, ct);
            var balance = balances.FirstOrDefault(b => Match(b, line.LocationId, line.BatchNo, line.SerialNo));

            if (balance is null)
            {
                balance = new StockBalance
                {
                    ItemId = line.ItemId,
                    WarehouseId = v.WarehouseId,
                    LocationId = line.LocationId,
                    BatchNo = line.BatchNo,
                    SerialNo = line.SerialNo,
                    ExpiryDate = line.ExpiryDate,
                    QtyOnHand = accepted,
                    QtyReserved = 0,
                    AvgCost = line.UnitCost,
                };
                db.StockBalances.Add(balance);
                balances.Add(balance);
            }
            else
            {
                var newQty = balance.QtyOnHand + accepted;
                balance.AvgCost = newQty == 0 ? line.UnitCost
                    : decimal.Round((balance.QtyOnHand * balance.AvgCost + accepted * line.UnitCost) / newQty, 4);
                balance.QtyOnHand = newQty;
            }

            var wac = Wac(balances);
            AddTxn(v, line, StockTxnType.Receipt, +1, accepted, line.UnitCost, balance.QtyOnHand, wac, v.WarehouseId,
                line.LocationId, line.BatchNo, line.SerialNo, line.ExpiryDate, user);

            await UpdateItemGlobalWacAsync(line.ItemId, v.WarehouseId, balances, line.UnitCost, ct);
        }

        return Result.Success();
    }

    // ─────────────────────────── الصرف / خروج (Issue-like) ───────────────────────────
    private async Task<Result> PostIssueLikeAsync(Voucher v, string user, StockTxnType txnType, CancellationToken ct)
    {
        var wh = await db.Warehouses.FirstOrDefaultAsync(w => w.Id == v.WarehouseId, ct);
        if (wh is null || wh.Status != WarehouseStatus.Active)
            return Result.Failure(Error.Validation("Warehouse", "لا يمكن الصرف من مخزن مغلق/مجمّد."));

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        foreach (var line in v.Lines.OrderBy(l => l.LineNo))
        {
            var item = await db.Items.FirstOrDefaultAsync(i => i.Id == line.ItemId, ct);
            if (item is null) return Result.Failure(Error.NotFound("Item", "الصنف غير موجود."));

            var balances = await LoadBalancesAsync(line.ItemId, v.WarehouseId, ct);

            // المرشّحون: متاح > 0 وغير منتهٍ.
            var candidates = balances
                .Where(b => b.QtyOnHand - b.QtyReserved > 0 && (b.ExpiryDate is null || b.ExpiryDate >= today))
                .ToList();

            candidates = item.TracksExpiry
                ? candidates.OrderBy(b => b.ExpiryDate ?? DateOnly.MaxValue).ThenBy(b => b.CreatedAt).ToList() // FEFO
                : candidates.OrderBy(b => b.CreatedAt).ToList();                                                 // FIFO

            var available = candidates.Sum(b => b.QtyOnHand - b.QtyReserved);
            if (available < line.Qty && !wh.AllowNegativeStock)
                return Result.Failure(Error.Validation("Stock",
                    $"الرصيد المتاح ({available}) غير كافٍ لصرف {line.Qty} من الصنف {item.ItemCode}."));

            var remaining = line.Qty;
            decimal allocatedValue = 0;
            foreach (var b in candidates)
            {
                if (remaining <= 0) break;
                var take = Math.Min(remaining, b.QtyOnHand - b.QtyReserved);
                if (take <= 0) continue;

                b.QtyOnHand -= take;
                allocatedValue += take * b.AvgCost;

                var wacAfter = Wac(balances);
                AddTxn(v, line, txnType, -1, take, b.AvgCost, b.QtyOnHand, wacAfter, v.WarehouseId,
                    b.LocationId, b.BatchNo, b.SerialNo, b.ExpiryDate, user);

                remaining -= take;
            }

            if (remaining > 0 && !wh.AllowNegativeStock)
                return Result.Failure(Error.Validation("Stock", "تعذّر تخصيص كامل الكمية المطلوبة."));

            line.UnitCost = line.Qty > 0 ? decimal.Round(allocatedValue / line.Qty, 4) : 0;
        }

        return Result.Success();
    }

    // ─────────────────────────── التحويل: خروج من المصدر ───────────────────────────
    private async Task<Result> PostTransferOutAsync(Voucher v, string user, CancellationToken ct)
    {
        if (v.ToWarehouseId is null || v.ToWarehouseId == v.WarehouseId)
            return Result.Failure(Error.Validation("Transfer", "مخزن الهدف غير صالح."));

        var result = await PostIssueLikeAsync(v, user, StockTxnType.TransferOut, ct);
        if (result.IsFailure) return result;

        v.TransferStatus = TransferStatus.InTransit;
        return Result.Success();
    }

    // ─────────────────────────── التحويل: تأكيد الاستلام في الهدف ───────────────────────────
    public async Task<Result> ConfirmTransferReceiptAsync(Voucher v, string user, CancellationToken ct)
    {
        if (v.VoucherType != VoucherType.Transfer || v.TransferStatus != TransferStatus.InTransit)
            return Result.Failure(Error.Validation("Transfer", "لا يوجد تحويل قيد النقل لتأكيده."));

        var freeze = await EnsureNotFrozenAsync(v.ToWarehouseId!.Value, ct);
        if (freeze.IsFailure) return freeze;

        var target = await db.Warehouses.FirstOrDefaultAsync(w => w.Id == v.ToWarehouseId, ct);
        if (target is null || target.Status != WarehouseStatus.Active)
            return Result.Failure(Error.Validation("Warehouse", "مخزن الهدف مغلق/مجمّد."));

        // حركات الخروج (TransferOut) لهذا السند = ما يدخل الهدف بنفس التكلفة.
        var outTxns = await db.StockTransactions
            .Where(t => t.VoucherId == v.Id && t.TxnType == StockTxnType.TransferOut)
            .ToListAsync(ct);

        foreach (var ot in outTxns)
        {
            var balances = await LoadBalancesAsync(ot.ItemId, target.Id, ct);
            var balance = balances.FirstOrDefault(b => Match(b, null, ot.BatchNo, ot.SerialNo));

            if (balance is null)
            {
                balance = new StockBalance
                {
                    ItemId = ot.ItemId,
                    WarehouseId = target.Id,
                    LocationId = null,
                    BatchNo = ot.BatchNo,
                    SerialNo = ot.SerialNo,
                    ExpiryDate = ot.ExpiryDate,
                    QtyOnHand = ot.Qty,
                    AvgCost = ot.UnitCost,
                };
                db.StockBalances.Add(balance);
                balances.Add(balance);
            }
            else
            {
                var newQty = balance.QtyOnHand + ot.Qty;
                balance.AvgCost = newQty == 0 ? ot.UnitCost
                    : decimal.Round((balance.QtyOnHand * balance.AvgCost + ot.Qty * ot.UnitCost) / newQty, 4);
                balance.QtyOnHand = newQty;
            }

            var wac = Wac(balances);
            AddTxnRaw(v, ot.VoucherLineId, ot.ItemId, StockTxnType.TransferIn, +1, ot.Qty, ot.UnitCost, balance.QtyOnHand, wac,
                target.Id, null, ot.BatchNo, ot.SerialNo, ot.ExpiryDate, user);

            await UpdateItemGlobalWacAsync(ot.ItemId, target.Id, balances, ot.UnitCost, ct);
        }

        v.TransferStatus = TransferStatus.Received;
        return Result.Success();
    }

    // ─────────────────────────── المرتجع ───────────────────────────
    private async Task<Result> PostReturnAsync(Voucher v, string user, CancellationToken ct)
    {
        var source = v.SourceVoucherId is null ? null
            : await db.Vouchers.FirstOrDefaultAsync(x => x.Id == v.SourceVoucherId, ct);
        if (source is null)
            return Result.Failure(Error.Validation("Return", "السند الأصلي للمرتجع مطلوب."));

        // مرتجع من طالب (عكس صرف) → إدخال؛ مرتجع لمورّد (عكس استلام) → إخراج.
        if (source.VoucherType == VoucherType.Issue)
            return await PostReturnInAsync(v, user, ct);

        return await PostIssueLikeAsync(v, user, StockTxnType.ReturnOut, ct);
    }

    private async Task<Result> PostReturnInAsync(Voucher v, string user, CancellationToken ct)
    {
        var wh = await db.Warehouses.FirstOrDefaultAsync(w => w.Id == v.WarehouseId, ct);
        if (wh is null || wh.Status != WarehouseStatus.Active)
            return Result.Failure(Error.Validation("Warehouse", "مخزن المرتجع مغلق/مجمّد."));

        foreach (var line in v.Lines.OrderBy(l => l.LineNo))
        {
            var balances = await LoadBalancesAsync(line.ItemId, v.WarehouseId, ct);
            var balance = balances.FirstOrDefault(b => Match(b, line.LocationId, line.BatchNo, line.SerialNo));

            if (balance is null)
            {
                balance = new StockBalance
                {
                    ItemId = line.ItemId,
                    WarehouseId = v.WarehouseId,
                    LocationId = line.LocationId,
                    BatchNo = line.BatchNo,
                    SerialNo = line.SerialNo,
                    ExpiryDate = line.ExpiryDate,
                    QtyOnHand = line.Qty,
                    AvgCost = line.UnitCost,
                };
                db.StockBalances.Add(balance);
                balances.Add(balance);
            }
            else
            {
                var newQty = balance.QtyOnHand + line.Qty;
                balance.AvgCost = newQty == 0 ? line.UnitCost
                    : decimal.Round((balance.QtyOnHand * balance.AvgCost + line.Qty * line.UnitCost) / newQty, 4);
                balance.QtyOnHand = newQty;
            }

            var wac = Wac(balances);
            AddTxn(v, line, StockTxnType.ReturnIn, +1, line.Qty, line.UnitCost, balance.QtyOnHand, wac, v.WarehouseId,
                line.LocationId, line.BatchNo, line.SerialNo, line.ExpiryDate, user);

            await UpdateItemGlobalWacAsync(line.ItemId, v.WarehouseId, balances, line.UnitCost, ct);
        }

        return Result.Success();
    }

    // ─────────────────────────── التسوية ───────────────────────────
    private async Task<Result> PostAdjustmentAsync(Voucher v, string user, CancellationToken ct)
    {
        var wh = await db.Warehouses.FirstOrDefaultAsync(w => w.Id == v.WarehouseId, ct);
        if (wh is null || wh.Status != WarehouseStatus.Active)
            return Result.Failure(Error.Validation("Warehouse", "مخزن التسوية مغلق/مجمّد."));

        var increase = v.AdjustmentType == AdjustmentType.IncreaseFound;

        foreach (var line in v.Lines.OrderBy(l => l.LineNo))
        {
            var balances = await LoadBalancesAsync(line.ItemId, v.WarehouseId, ct);
            var balance = balances.FirstOrDefault(b => Match(b, line.LocationId, line.BatchNo, line.SerialNo));

            if (increase)
            {
                if (balance is null)
                {
                    balance = new StockBalance
                    {
                        ItemId = line.ItemId, WarehouseId = v.WarehouseId, LocationId = line.LocationId,
                        BatchNo = line.BatchNo, SerialNo = line.SerialNo, ExpiryDate = line.ExpiryDate,
                        QtyOnHand = line.Qty, AvgCost = line.UnitCost,
                    };
                    db.StockBalances.Add(balance);
                    balances.Add(balance);
                }
                else
                {
                    var newQty = balance.QtyOnHand + line.Qty;
                    balance.AvgCost = newQty == 0 ? line.UnitCost
                        : decimal.Round((balance.QtyOnHand * balance.AvgCost + line.Qty * line.UnitCost) / newQty, 4);
                    balance.QtyOnHand = newQty;
                }

                var wacInc = Wac(balances);
                AddTxn(v, line, StockTxnType.AdjustIncrease, +1, line.Qty, line.UnitCost, balance.QtyOnHand, wacInc,
                    v.WarehouseId, line.LocationId, line.BatchNo, line.SerialNo, line.ExpiryDate, user);

                await UpdateItemGlobalWacAsync(line.ItemId, v.WarehouseId, balances, line.UnitCost, ct);
            }
            else
            {
                if (balance is null || balance.QtyOnHand - balance.QtyReserved < line.Qty)
                    return Result.Failure(Error.Validation("Stock", "الرصيد غير كافٍ لتسوية العجز/التلف."));

                balance.QtyOnHand -= line.Qty;
                var cost = balance.AvgCost;
                var wacDec = Wac(balances);
                AddTxn(v, line, StockTxnType.AdjustDecrease, -1, line.Qty, cost, balance.QtyOnHand, wacDec,
                    v.WarehouseId, line.LocationId, line.BatchNo, line.SerialNo, line.ExpiryDate, user);
            }
        }

        return Result.Success();
    }

    // ─────────────────────────── مساعدات ───────────────────────────
    private async Task<List<StockBalance>> LoadBalancesAsync(Guid itemId, Guid warehouseId, CancellationToken ct)
        => await db.StockBalances
            .Where(b => b.ItemId == itemId && b.WarehouseId == warehouseId)
            .ToListAsync(ct);

    private static bool Match(StockBalance b, Guid? locationId, string? batch, string? serial)
        => b.LocationId == locationId
           && string.Equals(b.BatchNo, batch, StringComparison.OrdinalIgnoreCase)
           && string.Equals(b.SerialNo, serial, StringComparison.OrdinalIgnoreCase);

    private static decimal Wac(IReadOnlyCollection<StockBalance> balances)
    {
        var qty = balances.Sum(b => b.QtyOnHand);
        if (qty <= 0) return 0;
        var value = balances.Sum(b => b.QtyOnHand * b.AvgCost);
        return decimal.Round(value / qty, 4);
    }

    /// <summary>
    /// يحدّث WAC العام للصنف (عبر كل المخازن) = أرصدة المخزن الحالي المُحدَّثة في الذاكرة
    /// + أرصدة بقية المخازن من القاعدة (لتفادي عدم رؤية التغييرات غير المحفوظة).
    /// </summary>
    private async Task UpdateItemGlobalWacAsync(
        Guid itemId, Guid warehouseId, IReadOnlyCollection<StockBalance> currentWhBalances,
        decimal lastCost, CancellationToken ct)
    {
        var item = await db.Items.FirstOrDefaultAsync(i => i.Id == itemId, ct);
        if (item is null) return;

        var others = await db.StockBalances.AsNoTracking()
            .Where(b => b.ItemId == itemId && b.WarehouseId != warehouseId)
            .ToListAsync(ct);

        var qty = currentWhBalances.Sum(b => b.QtyOnHand) + others.Sum(b => b.QtyOnHand);
        var value = currentWhBalances.Sum(b => b.QtyOnHand * b.AvgCost) + others.Sum(b => b.QtyOnHand * b.AvgCost);

        item.WeightedAvgCost = qty > 0 ? decimal.Round(value / qty, 4) : 0;
        item.LastPurchaseCost = lastCost;
    }

    private void AddTxn(Voucher v, VoucherLine line, StockTxnType type, short dir, decimal qty, decimal unitCost,
        decimal qtyAfter, decimal wacAfter, Guid warehouseId, Guid? locationId, string? batch, string? serial,
        DateOnly? expiry, string user)
        => AddTxnRaw(v, line.Id, line.ItemId, type, dir, qty, unitCost, qtyAfter, wacAfter, warehouseId, locationId, batch, serial, expiry, user);

    private void AddTxnRaw(Voucher v, Guid? lineId, Guid itemId, StockTxnType type, short dir, decimal qty, decimal unitCost,
        decimal qtyAfter, decimal wacAfter, Guid warehouseId, Guid? locationId, string? batch, string? serial,
        DateOnly? expiry, string user)
    {
        db.StockTransactions.Add(new StockTransaction
        {
            VoucherId = v.Id,
            VoucherLineId = lineId,
            TxnType = type,
            Direction = dir,
            ItemId = itemId,
            WarehouseId = warehouseId,
            LocationId = locationId,
            BatchNo = batch,
            SerialNo = serial,
            ExpiryDate = expiry,
            Qty = qty,
            UnitCost = unitCost,
            TotalCost = decimal.Round(qty * unitCost, 4),
            QtyOnHandAfter = qtyAfter,
            WacAfter = wacAfter,
            PostedAt = DateTime.UtcNow,
            PostedBy = user,
        });
    }
}
