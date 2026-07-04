using Microsoft.EntityFrameworkCore;
using WIMS.Application.Common.Interfaces;
using WIMS.Domain.Enums;
using WIMS.Domain.Transactions;
using WIMS.Shared.Results;
using CustodyEntity = WIMS.Domain.Custody.Custody;
using CustodyItemEntity = WIMS.Domain.Custody.CustodyItem;

namespace WIMS.Application.Features.Custody;

public sealed class CustodyProvisioningService(IAppDbContext db) : ICustodyProvisioningService
{
    public async Task<Result> ProvisionAsync(Voucher voucher, string userName, CancellationToken ct)
    {
        if (voucher.VoucherType != VoucherType.Issue)
            return Result.Success();

        if (voucher.RecipientEmployeeId is null)
            return Result.Success(); // لا مستلِم → لا عهدة (التحقق يمنع المستديم بلا مستلِم).

        // حركات الصرف المُنشأة لهذا السند (ما زالت في الذاكرة قبل الحفظ).
        var issueTxns = db.StockTransactions.Local
            .Where(t => t.VoucherId == voucher.Id && t.TxnType == StockTxnType.Issue)
            .ToList();
        if (issueTxns.Count == 0)
            return Result.Success();

        var itemIds = issueTxns.Select(t => t.ItemId).Distinct().ToList();
        var items = await db.Items.Where(i => itemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, ct);

        var custodyItems = issueTxns
            .Where(t => items.TryGetValue(t.ItemId, out var it) && it.ItemType == ItemType.Durable && it.RequiresCustody)
            .ToList();
        if (custodyItems.Count == 0)
            return Result.Success();

        var custody = await GetOrCreateActiveCustodyAsync(voucher.RecipientEmployeeId.Value, ct);
        var now = DateTime.UtcNow;

        foreach (var txn in custodyItems)
        {
            // إضافة صريحة عبر db.CustodyItems.Add (لا عبر custody.Items.Add على أبٍ
            // متتبَّع مسبقاً) — نفس فخّ EF الموثَّق في FreezeStockCountHandler: مفتاح Guid
            // مُولَّد من العميل (BaseEntity.Id) عبر تنقّل أبٍ متتبَّع يجعل EF يعتبر الابن
            // Modified فيحاول UPDATE بلا صفوف مطابقة → DbUpdateConcurrencyException.
            db.CustodyItems.Add(new CustodyItemEntity
            {
                CustodyId = custody.Id,
                ItemId = txn.ItemId,
                SerialNo = txn.SerialNo,
                Qty = txn.Qty,
                SourceStockTransactionId = txn.Id,
                SourceVoucherId = voucher.Id,
                UnitCost = txn.UnitCost,
                Status = CustodyItemStatus.InCustody,
                AssignedAt = now,
            });
        }

        return Result.Success();
    }

    private async Task<CustodyEntity> GetOrCreateActiveCustodyAsync(Guid employeeId, CancellationToken ct)
    {
        var existing = await db.Custodies
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.EmployeeId == employeeId && c.Status == CustodyStatus.Active, ct);
        if (existing is not null)
            return existing;

        var year = DateTime.UtcNow.Year;
        var count = await db.Custodies.CountAsync(ct);
        var custody = new CustodyEntity
        {
            CustodyNo = $"CUS-{year}-{(count + 1):D6}",
            CustodyType = CustodyType.Personal,
            EmployeeId = employeeId,
            Status = CustodyStatus.Active,
            OpenedAt = DateTime.UtcNow,
        };
        db.Custodies.Add(custody);
        return custody;
    }
}
