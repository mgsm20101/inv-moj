using MediatR;
using Microsoft.EntityFrameworkCore;
using WIMS.Application.Common.Interfaces;
using WIMS.Application.Common.Messaging;

namespace WIMS.Application.Features.Transactions.Queries;

public sealed record StockBalanceDto(
    Guid ItemId, string ItemCode, string ItemName, Guid WarehouseId, string WarehouseCode,
    string? BatchNo, string? SerialNo, DateOnly? ExpiryDate,
    decimal QtyOnHand, decimal QtyReserved, decimal QtyAvailable, decimal AvgCost);

/// <summary>أرصدة المخزون مع فلترة اختيارية بالمخزن/الصنف (غير الأصفار افتراضياً).</summary>
public sealed record GetStockBalancesQuery(
    Guid? WarehouseId = null, Guid? ItemId = null, bool IncludeZero = false)
    : IQuery<IReadOnlyList<StockBalanceDto>>;

public sealed class GetStockBalancesHandler(IAppDbContext db)
    : IRequestHandler<GetStockBalancesQuery, IReadOnlyList<StockBalanceDto>>
{
    public async Task<IReadOnlyList<StockBalanceDto>> Handle(GetStockBalancesQuery request, CancellationToken ct)
    {
        var q = db.StockBalances.AsNoTracking().AsQueryable();
        if (request.WarehouseId.HasValue) q = q.Where(b => b.WarehouseId == request.WarehouseId);
        if (request.ItemId.HasValue) q = q.Where(b => b.ItemId == request.ItemId);
        if (!request.IncludeZero) q = q.Where(b => b.QtyOnHand != 0);

        return await q
            .OrderBy(b => b.Item.ItemCode)
            .Select(b => new StockBalanceDto(
                b.ItemId, b.Item.ItemCode, b.Item.NameAr, b.WarehouseId, b.Warehouse.Code,
                b.BatchNo, b.SerialNo, b.ExpiryDate,
                b.QtyOnHand, b.QtyReserved, b.QtyAvailable, b.AvgCost))
            .ToListAsync(ct);
    }
}

// ─────────────────────── الدفتر (Ledger) ───────────────────────
public sealed record LedgerEntryDto(
    long TransactionNo, string VoucherNo, string TxnType, Guid ItemId, string ItemCode,
    Guid WarehouseId, string? BatchNo, decimal Qty, short Direction, decimal UnitCost,
    decimal QtyOnHandAfter, decimal WacAfter, DateTime PostedAt);

public sealed record GetLedgerQuery(Guid? ItemId = null, Guid? WarehouseId = null, int Take = 100)
    : IQuery<IReadOnlyList<LedgerEntryDto>>;

public sealed class GetLedgerHandler(IAppDbContext db)
    : IRequestHandler<GetLedgerQuery, IReadOnlyList<LedgerEntryDto>>
{
    public async Task<IReadOnlyList<LedgerEntryDto>> Handle(GetLedgerQuery request, CancellationToken ct)
    {
        var q = db.StockTransactions.AsNoTracking().AsQueryable();
        if (request.ItemId.HasValue) q = q.Where(t => t.ItemId == request.ItemId);
        if (request.WarehouseId.HasValue) q = q.Where(t => t.WarehouseId == request.WarehouseId);

        var take = request.Take is < 1 or > 1000 ? 100 : request.Take;

        return await q
            .OrderByDescending(t => t.TransactionNo)
            .Take(take)
            .Select(t => new LedgerEntryDto(
                t.TransactionNo, t.Voucher.VoucherNo, t.TxnType.ToString(), t.ItemId, t.Item.ItemCode,
                t.WarehouseId, t.BatchNo, t.Qty, t.Direction, t.UnitCost,
                t.QtyOnHandAfter, t.WacAfter, t.PostedAt))
            .ToListAsync(ct);
    }
}
