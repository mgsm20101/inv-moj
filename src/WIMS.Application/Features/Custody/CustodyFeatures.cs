using MediatR;
using Microsoft.EntityFrameworkCore;
using WIMS.Application.Common.Interfaces;
using WIMS.Application.Common.Messaging;
using WIMS.Application.Features.Transactions.Posting;
using WIMS.Domain.Enums;
using WIMS.Domain.Transactions;
using WIMS.Shared.Results;

namespace WIMS.Application.Features.Custody;

public sealed record CustodyItemLineDto(
    Guid CustodyItemId, string ItemCode, string ItemName, string? SerialNo,
    decimal Qty, decimal UnitCost, decimal Value, DateTime AssignedAt, CustodyItemStatus Status);

public sealed record CustodyStatementDto(
    Guid EmployeeId, string EmployeeNo, string EmployeeName, EmployeeStatus EmployeeStatus,
    int ItemCount, decimal TotalValue, IReadOnlyList<CustodyItemLineDto> Items);

// ─────────────────────── كشف عهدة موظف ───────────────────────
public sealed record GetCustodyStatementQuery(Guid EmployeeId, bool IncludeHistory = false)
    : IQuery<Result<CustodyStatementDto>>;

public sealed class GetCustodyStatementHandler(IAppDbContext db)
    : IRequestHandler<GetCustodyStatementQuery, Result<CustodyStatementDto>>
{
    public async Task<Result<CustodyStatementDto>> Handle(GetCustodyStatementQuery request, CancellationToken ct)
    {
        var emp = await db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == request.EmployeeId, ct);
        if (emp is null) return Error.NotFound("Employee", "الموظف غير موجود.");

        var itemsQuery = db.CustodyItems.AsNoTracking()
            .Where(ci => db.Custodies.Any(c => c.Id == ci.CustodyId && c.EmployeeId == request.EmployeeId));

        if (!request.IncludeHistory)
            itemsQuery = itemsQuery.Where(ci => ci.Status == CustodyItemStatus.InCustody);

        var items = await itemsQuery
            .OrderBy(ci => ci.Item.ItemCode)
            .Select(ci => new CustodyItemLineDto(
                ci.Id, ci.Item.ItemCode, ci.Item.NameAr, ci.SerialNo, ci.Qty, ci.UnitCost,
                ci.Qty * ci.UnitCost, ci.AssignedAt, ci.Status))
            .ToListAsync(ct);

        var inCustody = items.Where(i => i.Status == CustodyItemStatus.InCustody).ToList();
        var statement = new CustodyStatementDto(
            emp.Id, emp.EmployeeNo, emp.FullNameAr, emp.Status,
            inCustody.Count, inCustody.Sum(i => i.Value), items);

        return statement;
    }
}

// ─────────────────────── براءة ذمة (BR-CUS-05) ───────────────────────
public sealed record ClearEmployeeCustodyCommand(Guid EmployeeId) : ICommand<Result>;

public sealed class ClearEmployeeCustodyHandler(IAppDbContext db)
    : IRequestHandler<ClearEmployeeCustodyCommand, Result>
{
    public async Task<Result> Handle(ClearEmployeeCustodyCommand request, CancellationToken ct)
    {
        var custody = await db.Custodies
            .FirstOrDefaultAsync(c => c.EmployeeId == request.EmployeeId && c.Status == CustodyStatus.Active, ct);
        if (custody is null)
            return Result.Success(); // لا عهدة نشطة → براءة تلقائية.

        var open = await db.CustodyItems
            .CountAsync(ci => ci.CustodyId == custody.Id && ci.Status == CustodyItemStatus.InCustody, ct);
        if (open > 0)
            return Result.Failure(Error.Conflict("Custody.NotEmpty",
                $"تعذّرت براءة الذمة — يوجد {open} بند عهدة قائم يجب إرجاعه/نقله أولاً."));

        custody.Status = CustodyStatus.Cleared;
        custody.ClosedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ─────────────────────── إرجاع بند عهدة للمخزن ───────────────────────
/// <summary>
/// يُرجع بند عهدة واحداً للمخزن الذي صُرف منه أصلاً: يبني سند مرتجع (Return) معتمَداً
/// فوراً (إجراء عهدة إداري كـ "براءة ذمة"، لا يمرّ بدورة مسوّدة/اعتماد منفصلة)، يُرحَّله
/// عبر محرّك الترحيل نفسه (يعيد الكمية فعلياً لرصيد المخزن)، ويُغلق بند العهدة (Returned).
/// </summary>
public sealed record ReturnCustodyItemCommand(Guid CustodyItemId) : ICommand<Result>;

public sealed class ReturnCustodyItemHandler(
    IAppDbContext db, IVoucherPostingService posting, ICurrentUser user)
    : IRequestHandler<ReturnCustodyItemCommand, Result>
{
    public async Task<Result> Handle(ReturnCustodyItemCommand request, CancellationToken ct)
    {
        var item = await db.CustodyItems.FirstOrDefaultAsync(ci => ci.Id == request.CustodyItemId, ct);
        if (item is null) return Result.Failure(Error.NotFound("CustodyItem", "بند العهدة غير موجود."));
        if (item.Status != CustodyItemStatus.InCustody)
            return Result.Failure(Error.Validation("CustodyItem.Status", "بند العهدة ليس قائماً حالياً؛ رُدّ أو نُقل من قبل."));

        var sourceTxn = await db.StockTransactions
            .FirstOrDefaultAsync(t => t.Id == item.SourceStockTransactionId, ct);
        if (sourceTxn is null)
            return Result.Failure(Error.NotFound("StockTransaction", "تعذّر تحديد المخزن الأصلي لبند العهدة."));

        var currentUser = user.UserName ?? "system";
        var voucher = new Voucher
        {
            VoucherType = VoucherType.Return,
            Status = VoucherStatus.Approved,
            WarehouseId = sourceTxn.WarehouseId,
            SourceVoucherId = item.SourceVoucherId,
            Reason = "إرجاع عهدة للمخزن",
            SubmittedBy = currentUser,
            SubmittedAt = DateTime.UtcNow,
            ApprovedBy = currentUser,
            ApprovedAt = DateTime.UtcNow,
        };
        voucher.Lines.Add(new VoucherLine
        {
            LineNo = 1,
            ItemId = item.ItemId,
            Qty = item.Qty,
            QtyAccepted = item.Qty,
            SerialNo = item.SerialNo,
            UnitCost = item.UnitCost,
        });
        db.Vouchers.Add(voucher);

        var posted = await posting.PostAsync(voucher, currentUser, ct);
        if (posted.IsFailure) return posted;

        voucher.PostedAt = DateTime.UtcNow;
        voucher.VoucherNo = await GenerateVoucherNoAsync(ct);
        item.Status = CustodyItemStatus.Returned;

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(Error.Conflict("Stock.Concurrency",
                "تعذّر إتمام الإرجاع بسبب تحديث متزامن على الرصيد؛ أعد المحاولة."));
        }

        return Result.Success();
    }

    private async Task<string> GenerateVoucherNoAsync(CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        var count = await db.Vouchers.CountAsync(
            v => v.VoucherType == VoucherType.Return && v.Status == VoucherStatus.Approved, ct);
        return $"RTN-{year}-{(count + 1):D6}";
    }
}
