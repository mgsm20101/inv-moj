using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using WIMS.Application.Common.Interfaces;
using WIMS.Application.Common.Messaging;
using WIMS.Application.Features.Transactions.Posting;
using WIMS.Domain.Enums;
using WIMS.Domain.Inventory;
using WIMS.Domain.Transactions;
using WIMS.Shared.Results;

namespace WIMS.Application.Features.StockCounts;

// ═══════════════════════════ DTOs ═══════════════════════════
public sealed record StockCountLineDto(
    Guid Id, int LineNo, Guid ItemId, string ItemCode, string ItemName,
    Guid? LocationId, string? BatchNo, string? SerialNo, DateOnly? ExpiryDate,
    decimal BookQty, decimal? PhysicalQty, decimal VarianceQty,
    decimal UnitCost, decimal VarianceValue, bool Counted);

public sealed record StockCountDto(
    Guid Id, string CountNo, StockCountType CountType, StockCountStatus Status,
    Guid WarehouseId, string WarehouseName, string? ScopeNote,
    DateTime? FrozenAt, string? FrozenBy, DateTime? CountedAt,
    string? ApprovedBy, DateTime? ApprovedAt, string? AdjustmentVoucherNos,
    int LineCount, decimal TotalVarianceValue);

public sealed record StockCountDetailDto(StockCountDto Header, IReadOnlyList<StockCountLineDto> Lines);

// ═══════════════════════════ 1) تخطيط الجرد (Draft) ═══════════════════════════
public sealed record PlanStockCountCommand : ICommand<Result<Guid>>
{
    public Guid WarehouseId { get; init; }
    public StockCountType CountType { get; init; } = StockCountType.Full;
    public IReadOnlyList<Guid> ItemIds { get; init; } = [];
    public string? ScopeNote { get; init; }
}

public sealed class PlanStockCountValidator : AbstractValidator<PlanStockCountCommand>
{
    public PlanStockCountValidator()
    {
        RuleFor(x => x.WarehouseId).NotEmpty().WithMessage("المخزن مطلوب.");
        RuleFor(x => x.ItemIds).NotEmpty()
            .When(x => x.CountType == StockCountType.Partial)
            .WithMessage("الجرد الجزئي يتطلّب تحديد أصناف.");
    }
}

public sealed class PlanStockCountHandler(IAppDbContext db) : IRequestHandler<PlanStockCountCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(PlanStockCountCommand request, CancellationToken ct)
    {
        var wh = await db.Warehouses.FirstOrDefaultAsync(w => w.Id == request.WarehouseId, ct);
        if (wh is null) return Error.NotFound("Warehouse", "المخزن غير موجود.");

        // جرد نشِط واحد فقط لكل مخزن (Draft/Frozen/UnderReview).
        var hasActive = await db.StockCounts.AnyAsync(c => c.WarehouseId == request.WarehouseId
            && (c.Status == StockCountStatus.Draft || c.Status == StockCountStatus.Frozen
                || c.Status == StockCountStatus.UnderReview), ct);
        if (hasActive)
            return Error.Conflict("StockCount.Active", "يوجد جرد نشِط لهذا المخزن — أكمِله أو ألغِه أولاً.");

        var count = new StockCount
        {
            CountNo = await GenerateCountNoAsync(ct),
            CountType = request.CountType,
            Status = StockCountStatus.Draft,
            WarehouseId = request.WarehouseId,
            ScopeNote = request.CountType == StockCountType.Partial && request.ItemIds.Count > 0
                ? string.Join(",", request.ItemIds)
                : request.ScopeNote?.Trim(),
        };
        db.StockCounts.Add(count);
        await db.SaveChangesAsync(ct);
        return count.Id;
    }

    private async Task<string> GenerateCountNoAsync(CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        var count = await db.StockCounts.CountAsync(ct);
        return $"CNT-{year}-{(count + 1):D6}";
    }
}

// ═══════════════════════════ 2) التجميد + لقطة الرصيد الدفتري ═══════════════════════════
public sealed record FreezeStockCountCommand(Guid Id) : ICommand<Result>;

public sealed class FreezeStockCountHandler(IAppDbContext db, ICurrentUser user)
    : IRequestHandler<FreezeStockCountCommand, Result>
{
    public async Task<Result> Handle(FreezeStockCountCommand request, CancellationToken ct)
    {
        var count = await db.StockCounts.Include(c => c.Lines)
            .FirstOrDefaultAsync(c => c.Id == request.Id, ct);
        if (count is null) return Result.Failure(Error.NotFound("StockCount", "محضر الجرد غير موجود."));
        if (count.Status != StockCountStatus.Draft)
            return Result.Failure(Error.Validation("StockCount.Status", "لا يُجمَّد إلا محضر في حالة مسودة."));

        // نطاق الأصناف للجرد الجزئي.
        var scopeItemIds = count.CountType == StockCountType.Partial && !string.IsNullOrWhiteSpace(count.ScopeNote)
            ? count.ScopeNote.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(Guid.Parse).ToHashSet()
            : null;

        var balancesQuery = db.StockBalances.AsNoTracking()
            .Where(b => b.WarehouseId == count.WarehouseId && b.QtyOnHand > 0);
        if (scopeItemIds is not null)
            balancesQuery = balancesQuery.Where(b => scopeItemIds.Contains(b.ItemId));

        var balances = await balancesQuery.ToListAsync(ct);

        // تُضاف الأسطر للـ DbSet مباشرةً (لا عبر التنقّل) لأن المحضر مُتتبَّع مسبقاً
        // ومفاتيح الأسطر مُولَّدة من العميل — الإضافة عبر التنقّل تجعل EF يعتبرها Modified.
        var lineNo = 1;
        foreach (var b in balances.OrderBy(b => b.ItemId).ThenBy(b => b.BatchNo))
        {
            db.StockCountLines.Add(new StockCountLine
            {
                StockCountId = count.Id,
                LineNo = lineNo++,
                ItemId = b.ItemId,
                LocationId = b.LocationId,
                BatchNo = b.BatchNo,
                SerialNo = b.SerialNo,
                ExpiryDate = b.ExpiryDate,
                BookQty = b.QtyOnHand,
                UnitCost = b.AvgCost,
                PhysicalQty = null,
                VarianceQty = 0,
                VarianceValue = 0,
                Counted = false,
            });
        }

        count.Status = StockCountStatus.Frozen;
        count.FrozenAt = DateTime.UtcNow;
        count.FrozenBy = user.UserName;
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ═══════════════════════════ 3) إدخال العدّ الفعلي ═══════════════════════════
public sealed record CountEntry(Guid LineId, decimal PhysicalQty);
public sealed record EnterCountCommand(Guid Id, IReadOnlyList<CountEntry> Entries) : ICommand<Result>;

public sealed class EnterCountValidator : AbstractValidator<EnterCountCommand>
{
    public EnterCountValidator()
    {
        RuleFor(x => x.Entries).NotEmpty().WithMessage("لا توجد قراءات للإدخال.");
        RuleForEach(x => x.Entries).Must(e => e.PhysicalQty >= 0)
            .WithMessage("العدّ الفعلي لا يكون سالباً.");
    }
}

public sealed class EnterCountHandler(IAppDbContext db, ICurrentUser user)
    : IRequestHandler<EnterCountCommand, Result>
{
    public async Task<Result> Handle(EnterCountCommand request, CancellationToken ct)
    {
        var count = await db.StockCounts.Include(c => c.Lines)
            .FirstOrDefaultAsync(c => c.Id == request.Id, ct);
        if (count is null) return Result.Failure(Error.NotFound("StockCount", "محضر الجرد غير موجود."));
        if (count.Status != StockCountStatus.Frozen)
            return Result.Failure(Error.Validation("StockCount.Status", "لا يُدخَل العدّ إلا لمحضر مُجمَّد."));

        var now = DateTime.UtcNow;
        foreach (var entry in request.Entries)
        {
            var line = count.Lines.FirstOrDefault(l => l.Id == entry.LineId);
            if (line is null)
                return Result.Failure(Error.NotFound("StockCountLine", $"سطر الجرد {entry.LineId} غير موجود."));

            line.PhysicalQty = entry.PhysicalQty;
            line.VarianceQty = entry.PhysicalQty - line.BookQty;
            line.VarianceValue = decimal.Round(line.VarianceQty * line.UnitCost, 4);
            line.Counted = true;
            line.CountedBy = user.UserName;
            line.CountedAt = now;
        }

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ═══════════════════════════ 4) اعتماد التقفيل (Frozen → UnderReview) ═══════════════════════════
public sealed record SubmitStockCountCommand(Guid Id) : ICommand<Result>;

public sealed class SubmitStockCountHandler(IAppDbContext db) : IRequestHandler<SubmitStockCountCommand, Result>
{
    public async Task<Result> Handle(SubmitStockCountCommand request, CancellationToken ct)
    {
        var count = await db.StockCounts.Include(c => c.Lines)
            .FirstOrDefaultAsync(c => c.Id == request.Id, ct);
        if (count is null) return Result.Failure(Error.NotFound("StockCount", "محضر الجرد غير موجود."));
        if (count.Status != StockCountStatus.Frozen)
            return Result.Failure(Error.Validation("StockCount.Status", "لا يُقفَل إلا محضر مُجمَّد."));

        // الأسطر غير المعدودة تُعتبر مطابِقة للدفتري (فرق صفري).
        foreach (var line in count.Lines.Where(l => !l.Counted))
        {
            line.PhysicalQty = line.BookQty;
            line.VarianceQty = 0;
            line.VarianceValue = 0;
            line.Counted = true;
        }

        count.Status = StockCountStatus.UnderReview;
        count.CountedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ═══════════════════════════ 5) الاعتماد + ترحيل التسويات (UnderReview → Approved) ═══════════════════════════
public sealed record ApproveStockCountCommand(Guid Id) : ICommand<Result>;

public sealed class ApproveStockCountHandler(IAppDbContext db, IVoucherPostingService posting, ICurrentUser user)
    : IRequestHandler<ApproveStockCountCommand, Result>
{
    public async Task<Result> Handle(ApproveStockCountCommand request, CancellationToken ct)
    {
        var count = await db.StockCounts.Include(c => c.Lines)
            .FirstOrDefaultAsync(c => c.Id == request.Id, ct);
        if (count is null) return Result.Failure(Error.NotFound("StockCount", "محضر الجرد غير موجود."));
        if (count.Status != StockCountStatus.UnderReview)
            return Result.Failure(Error.Validation("StockCount.Status", "لا يُعتمد إلا محضر قيد المراجعة."));

        var currentUser = user.UserName ?? "system";

        // فصل الواجبات: المعتمِد ≠ من جمّد المحضر أو أنشأه.
        if (string.Equals(count.FrozenBy, currentUser, StringComparison.OrdinalIgnoreCase)
            || string.Equals(count.CreatedBy, currentUser, StringComparison.OrdinalIgnoreCase))
            return Result.Failure(Error.Forbidden("StockCount.SoD",
                "لا يجوز اعتماد محضر جرد جمّدته أو أنشأته بنفسك (فصل الواجبات)."));

        // فكّ التجميد أولاً (بضبط الحالة) كي تمرّ تسويات الجرد عبر محرّك الترحيل.
        count.Status = StockCountStatus.Approved;
        count.ApprovedBy = currentUser;
        count.ApprovedAt = DateTime.UtcNow;

        var voucherNos = new List<string>();

        var increases = count.Lines.Where(l => l.VarianceQty > 0).ToList();
        var decreases = count.Lines.Where(l => l.VarianceQty < 0).ToList();

        if (increases.Count > 0)
        {
            var r = await BuildAndPostAsync(count, increases, AdjustmentType.IncreaseFound, currentUser, voucherNos, ct);
            if (r.IsFailure) return r;
        }
        if (decreases.Count > 0)
        {
            var r = await BuildAndPostAsync(count, decreases, AdjustmentType.DecreaseShortage, currentUser, voucherNos, ct);
            if (r.IsFailure) return r;
        }

        count.AdjustmentVoucherNos = voucherNos.Count > 0 ? string.Join(", ", voucherNos) : null;

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(Error.Conflict("Stock.Concurrency",
                "تعذّر ترحيل تسويات الجرد بسبب تحديث متزامن على الرصيد؛ أعد المحاولة."));
        }

        return Result.Success();
    }

    /// <summary>يبني سند تسوية من فروقات الجرد ويُرحّله عبر محرّك الترحيل نفسه.</summary>
    private async Task<Result> BuildAndPostAsync(
        StockCount count, IReadOnlyList<StockCountLine> lines, AdjustmentType type,
        string currentUser, List<string> voucherNos, CancellationToken ct)
    {
        var voucher = new Voucher
        {
            VoucherType = VoucherType.Adjustment,
            Status = VoucherStatus.Approved,
            WarehouseId = count.WarehouseId,
            AdjustmentType = type,
            Reason = $"تسوية جرد {count.CountNo}",
            SubmittedBy = currentUser,
            SubmittedAt = DateTime.UtcNow,
            ApprovedBy = currentUser,
            ApprovedAt = DateTime.UtcNow,
            PostedAt = DateTime.UtcNow,
        };

        var lineNo = 1;
        foreach (var l in lines)
        {
            voucher.Lines.Add(new VoucherLine
            {
                LineNo = lineNo++,
                ItemId = l.ItemId,
                LocationId = l.LocationId,
                BatchNo = l.BatchNo,
                SerialNo = l.SerialNo,
                ExpiryDate = l.ExpiryDate,
                Qty = Math.Abs(l.VarianceQty),
                UnitCost = l.UnitCost,
                Notes = $"سطر جرد {l.LineNo}",
            });
        }

        db.Vouchers.Add(voucher);
        var posted = await posting.PostAsync(voucher, currentUser, ct);
        if (posted.IsFailure) return posted;

        voucher.VoucherNo = await GenerateAdjustmentNoAsync(ct, voucherNos.Count);
        voucherNos.Add(voucher.VoucherNo);
        return Result.Success();
    }

    private async Task<string> GenerateAdjustmentNoAsync(CancellationToken ct, int offset)
    {
        var year = DateTime.UtcNow.Year;
        var count = await db.Vouchers.CountAsync(
            x => x.VoucherType == VoucherType.Adjustment && x.Status == VoucherStatus.Approved, ct);
        return $"ADJ-{year}-{(count + 1 + offset):D6}";
    }
}

// ═══════════════════════════ 6) الإلغاء (يفكّ التجميد) ═══════════════════════════
public sealed record CancelStockCountCommand(Guid Id) : ICommand<Result>;

public sealed class CancelStockCountHandler(IAppDbContext db) : IRequestHandler<CancelStockCountCommand, Result>
{
    public async Task<Result> Handle(CancelStockCountCommand request, CancellationToken ct)
    {
        var count = await db.StockCounts.FirstOrDefaultAsync(c => c.Id == request.Id, ct);
        if (count is null) return Result.Failure(Error.NotFound("StockCount", "محضر الجرد غير موجود."));
        if (count.Status is StockCountStatus.Approved or StockCountStatus.Cancelled)
            return Result.Failure(Error.Validation("StockCount.Status", "لا يُلغى محضر معتمد أو ملغى."));

        count.Status = StockCountStatus.Cancelled;
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ═══════════════════════════ استعلامات ═══════════════════════════
public sealed record GetStockCountsQuery(StockCountStatus? Status = null) : IQuery<IReadOnlyList<StockCountDto>>;

public sealed class GetStockCountsHandler(IAppDbContext db) : IRequestHandler<GetStockCountsQuery, IReadOnlyList<StockCountDto>>
{
    public async Task<IReadOnlyList<StockCountDto>> Handle(GetStockCountsQuery request, CancellationToken ct)
    {
        var q = db.StockCounts.AsNoTracking().AsQueryable();
        if (request.Status is not null)
            q = q.Where(c => c.Status == request.Status);

        return await q.OrderByDescending(c => c.CreatedAt)
            .Select(c => new StockCountDto(
                c.Id, c.CountNo, c.CountType, c.Status,
                c.WarehouseId, c.Warehouse.NameAr, c.ScopeNote,
                c.FrozenAt, c.FrozenBy, c.CountedAt, c.ApprovedBy, c.ApprovedAt, c.AdjustmentVoucherNos,
                c.Lines.Count, c.Lines.Sum(l => l.VarianceValue)))
            .ToListAsync(ct);
    }
}

public sealed record GetStockCountByIdQuery(Guid Id) : IQuery<Result<StockCountDetailDto>>;

public sealed class GetStockCountByIdHandler(IAppDbContext db)
    : IRequestHandler<GetStockCountByIdQuery, Result<StockCountDetailDto>>
{
    public async Task<Result<StockCountDetailDto>> Handle(GetStockCountByIdQuery request, CancellationToken ct)
    {
        var header = await db.StockCounts.AsNoTracking()
            .Where(x => x.Id == request.Id)
            .Select(c => new StockCountDto(
                c.Id, c.CountNo, c.CountType, c.Status, c.WarehouseId, c.Warehouse.NameAr, c.ScopeNote,
                c.FrozenAt, c.FrozenBy, c.CountedAt, c.ApprovedBy, c.ApprovedAt, c.AdjustmentVoucherNos,
                0, 0))
            .FirstOrDefaultAsync(ct);
        if (header is null) return Error.NotFound("StockCount", "محضر الجرد غير موجود.");

        var lines = await db.StockCountLines.AsNoTracking()
            .Where(l => l.StockCountId == request.Id)
            .OrderBy(l => l.LineNo)
            .Select(l => new StockCountLineDto(
                l.Id, l.LineNo, l.ItemId, l.Item.ItemCode, l.Item.NameAr,
                l.LocationId, l.BatchNo, l.SerialNo, l.ExpiryDate,
                l.BookQty, l.PhysicalQty, l.VarianceQty, l.UnitCost, l.VarianceValue, l.Counted))
            .ToListAsync(ct);

        header = header with { LineCount = lines.Count, TotalVarianceValue = lines.Sum(l => l.VarianceValue) };
        return new StockCountDetailDto(header, lines);
    }
}
