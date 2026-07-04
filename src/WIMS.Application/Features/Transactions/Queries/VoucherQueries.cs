using MediatR;
using Microsoft.EntityFrameworkCore;
using WIMS.Application.Common.Interfaces;
using WIMS.Application.Common.Messaging;
using WIMS.Application.Common.Models;
using WIMS.Domain.Enums;
using WIMS.Shared.Results;

namespace WIMS.Application.Features.Transactions.Queries;

public sealed record VoucherLineDto(
    int LineNo, Guid ItemId, string ItemCode, decimal Qty, decimal QtyAccepted,
    decimal QtyRejected, string? BatchNo, string? SerialNo, DateOnly? ExpiryDate, decimal UnitCost);

public sealed record VoucherDto(
    Guid Id, string VoucherNo, VoucherType VoucherType, VoucherStatus Status,
    Guid WarehouseId, Guid? ToWarehouseId, TransferStatus? TransferStatus,
    string? CreatedBy, string? ApprovedBy, DateTime? PostedAt, int LineCount);

public sealed record VoucherDetailDto(
    Guid Id, string VoucherNo, VoucherType VoucherType, VoucherStatus Status,
    Guid WarehouseId, Guid? ToWarehouseId, Guid? SupplierId, TransferStatus? TransferStatus,
    string? Reason, string? CreatedBy, string? ApprovedBy, DateTime? PostedAt, DateOnly? DocumentDate,
    IReadOnlyList<VoucherLineDto> Lines);

// ─────────────────────── قائمة السندات ───────────────────────
public sealed record GetVouchersQuery(
    VoucherType? Type = null, VoucherStatus? Status = null, int Page = 1, int PageSize = 20)
    : IQuery<PagedResult<VoucherDto>>;

public sealed class GetVouchersHandler(IAppDbContext db)
    : IRequestHandler<GetVouchersQuery, PagedResult<VoucherDto>>
{
    public async Task<PagedResult<VoucherDto>> Handle(GetVouchersQuery request, CancellationToken ct)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var size = request.PageSize is < 1 or > 200 ? 20 : request.PageSize;

        var q = db.Vouchers.AsNoTracking().AsQueryable();
        if (request.Type.HasValue) q = q.Where(v => v.VoucherType == request.Type);
        if (request.Status.HasValue) q = q.Where(v => v.Status == request.Status);

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(v => v.CreatedAt)
            .Skip((page - 1) * size).Take(size)
            .Select(v => new VoucherDto(
                v.Id, v.VoucherNo, v.VoucherType, v.Status, v.WarehouseId, v.ToWarehouseId,
                v.TransferStatus, v.CreatedBy, v.ApprovedBy, v.PostedAt, v.Lines.Count))
            .ToListAsync(ct);

        return new PagedResult<VoucherDto>(items, total, page, size);
    }
}

// ─────────────────────── تفاصيل سند ───────────────────────
public sealed record GetVoucherByIdQuery(Guid Id) : IQuery<Result<VoucherDetailDto>>;

public sealed class GetVoucherByIdHandler(IAppDbContext db)
    : IRequestHandler<GetVoucherByIdQuery, Result<VoucherDetailDto>>
{
    public async Task<Result<VoucherDetailDto>> Handle(GetVoucherByIdQuery request, CancellationToken ct)
    {
        var v = await db.Vouchers.AsNoTracking()
            .Where(x => x.Id == request.Id)
            .Select(x => new VoucherDetailDto(
                x.Id, x.VoucherNo, x.VoucherType, x.Status, x.WarehouseId, x.ToWarehouseId, x.SupplierId,
                x.TransferStatus, x.Reason, x.CreatedBy, x.ApprovedBy, x.PostedAt, x.DocumentDate,
                x.Lines.OrderBy(l => l.LineNo).Select(l => new VoucherLineDto(
                    l.LineNo, l.ItemId, l.Item.ItemCode, l.Qty, l.QtyAccepted, l.QtyRejected,
                    l.BatchNo, l.SerialNo, l.ExpiryDate, l.UnitCost)).ToList()))
            .FirstOrDefaultAsync(ct);

        return v is null ? Error.NotFound("Voucher", "السند غير موجود.") : v;
    }
}
