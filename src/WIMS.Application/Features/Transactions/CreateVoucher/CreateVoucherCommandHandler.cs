using MediatR;
using Microsoft.EntityFrameworkCore;
using WIMS.Application.Common.Interfaces;
using WIMS.Domain.Enums;
using WIMS.Domain.Transactions;
using WIMS.Shared.Results;

namespace WIMS.Application.Features.Transactions.CreateVoucher;

public sealed class CreateVoucherCommandHandler(IAppDbContext db)
    : IRequestHandler<CreateVoucherCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateVoucherCommand request, CancellationToken ct)
    {
        if (request.Lines.Count == 0)
            return Error.Validation("Voucher.Lines", "يجب أن يحتوي السند على سطر واحد على الأقل.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (request.DocumentDate is { } docDate && docDate > today)
            return Error.Validation("Voucher.DocumentDate", "تاريخ الإذن لا يمكن أن يكون في المستقبل.");

        var warehouse = await db.Warehouses.FirstOrDefaultAsync(w => w.Id == request.WarehouseId, ct);
        if (warehouse is null)
            return Error.NotFound("Warehouse", "المخزن غير موجود.");

        // ── تحقق حسب النوع ──
        var typeCheck = await ValidateByTypeAsync(request, ct);
        if (typeCheck.IsFailure)
            return Result.Failure<Guid>(typeCheck.Error);

        // ── تحميل الأصناف للتحقق من أبعاد التتبّع ──
        var itemIds = request.Lines.Select(l => l.ItemId).Distinct().ToList();
        var items = await db.Items.Where(i => itemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, ct);

        var voucher = new Voucher
        {
            VoucherNo = $"DRAFT-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
            VoucherType = request.VoucherType,
            Status = VoucherStatus.Draft,
            WarehouseId = request.WarehouseId,
            ToWarehouseId = request.ToWarehouseId,
            SupplierId = request.SupplierId,
            SourceVoucherId = request.SourceVoucherId,
            ReferenceNo = request.ReferenceNo?.Trim(),
            DocumentDate = request.DocumentDate ?? today,
            RequestingDept = request.RequestingDept?.Trim(),
            Reason = request.Reason?.Trim(),
            RecipientEmployeeId = request.RecipientEmployeeId,
            AdjustmentType = request.AdjustmentType,
            TransferStatus = request.VoucherType == VoucherType.Transfer ? TransferStatus.Draft : null,
            Notes = request.Notes?.Trim(),
        };

        var lineNo = 1;
        foreach (var input in request.Lines)
        {
            if (!items.TryGetValue(input.ItemId, out var item))
                return Error.NotFound("Item", $"الصنف {input.ItemId} غير موجود.");
            if (input.Qty <= 0)
                return Error.Validation("Line.Qty", "الكمية يجب أن تكون أكبر من صفر.");

            var accepted = input.QtyAccepted ?? input.Qty;
            var rejected = input.QtyRejected ?? 0;

            // تحقق أبعاد التتبّع للأنواع التي يُدخل فيها المستخدم الدُفعة (استلام/مرتجع/تسوية).
            if (request.VoucherType is VoucherType.Receipt or VoucherType.Return or VoucherType.Adjustment)
            {
                if (item.TracksBatch && string.IsNullOrWhiteSpace(input.BatchNo))
                    return Error.Validation("Line.Batch", $"الصنف {item.ItemCode} يتتبّع الدُفعة؛ رقم الدُفعة مطلوب.");
                if (item.TracksSerial && string.IsNullOrWhiteSpace(input.SerialNo))
                    return Error.Validation("Line.Serial", $"الصنف {item.ItemCode} يتتبّع السيريال؛ الرقم مطلوب.");
                if (item.TracksExpiry && input.ExpiryDate is null)
                    return Error.Validation("Line.Expiry", $"الصنف {item.ItemCode} يتتبّع الصلاحية؛ التاريخ مطلوب.");
            }

            if (request.VoucherType == VoucherType.Receipt)
            {
                if (accepted + rejected != input.Qty)
                    return Error.Validation("Line.Qty", "المقبول + المرفوض يجب أن يساوي الكمية المستلمة.");
                if (rejected > 0 && string.IsNullOrWhiteSpace(input.RejectReason))
                    return Error.Validation("Line.RejectReason", "سبب الرفض مطلوب عند وجود كمية مرفوضة.");
                if ((input.UnitCost ?? -1) < 0)
                    return Error.Validation("Line.UnitCost", "تكلفة الوحدة مطلوبة (≥ 0) في الاستلام.");
                if (item.TracksExpiry && input.ExpiryDate <= today)
                    return Error.Validation("Line.Expiry", "لا يمكن استلام صنف منتهي الصلاحية.");
            }

            voucher.Lines.Add(new VoucherLine
            {
                LineNo = lineNo++,
                ItemId = input.ItemId,
                LocationId = input.LocationId,
                ToLocationId = input.ToLocationId,
                Qty = input.Qty,
                QtyAccepted = request.VoucherType == VoucherType.Receipt ? accepted : input.Qty,
                QtyRejected = request.VoucherType == VoucherType.Receipt ? rejected : 0,
                RejectReason = input.RejectReason?.Trim(),
                BatchNo = string.IsNullOrWhiteSpace(input.BatchNo) ? null : input.BatchNo.Trim(),
                SerialNo = string.IsNullOrWhiteSpace(input.SerialNo) ? null : input.SerialNo.Trim(),
                ExpiryDate = input.ExpiryDate,
                UnitCost = input.UnitCost ?? 0,
                Notes = input.Notes?.Trim(),
            });
        }

        db.Vouchers.Add(voucher);
        await db.SaveChangesAsync(ct);
        return voucher.Id;
    }

    private async Task<Result> ValidateByTypeAsync(CreateVoucherCommand r, CancellationToken ct)
    {
        switch (r.VoucherType)
        {
            case VoucherType.Receipt:
                if (r.SupplierId is null)
                    return Result.Failure(Error.Validation("Supplier", "المورّد مطلوب في إذن الاستلام."));
                var supplierOk = await db.Suppliers.AnyAsync(s => s.Id == r.SupplierId && s.IsActive, ct);
                if (!supplierOk)
                    return Result.Failure(Error.Validation("Supplier", "المورّد غير موجود أو غير نشط."));
                break;

            case VoucherType.Issue:
                // لا تحقق إضافي — أُزيل اشتراط مركز التكلفة (حُذف من النظام).
                break;

            case VoucherType.Transfer:
                if (r.ToWarehouseId is null || r.ToWarehouseId == r.WarehouseId)
                    return Result.Failure(Error.Validation("ToWarehouse", "مخزن الهدف مطلوب ويجب أن يختلف عن المصدر."));
                if (!await db.Warehouses.AnyAsync(w => w.Id == r.ToWarehouseId, ct))
                    return Result.Failure(Error.NotFound("ToWarehouse", "مخزن الهدف غير موجود."));
                break;

            case VoucherType.Return:
                if (r.SourceVoucherId is null)
                    return Result.Failure(Error.Validation("Source", "السند الأصلي مطلوب في المرتجع."));
                if (string.IsNullOrWhiteSpace(r.Reason))
                    return Result.Failure(Error.Validation("Reason", "سبب المرتجع مطلوب."));
                if (!await db.Vouchers.AnyAsync(v => v.Id == r.SourceVoucherId, ct))
                    return Result.Failure(Error.NotFound("Source", "السند الأصلي غير موجود."));
                break;

            case VoucherType.Adjustment:
                if (r.AdjustmentType is null)
                    return Result.Failure(Error.Validation("AdjustmentType", "نوع التسوية مطلوب."));
                if (string.IsNullOrWhiteSpace(r.Reason))
                    return Result.Failure(Error.Validation("Reason", "سبب التسوية مطلوب."));
                break;
        }

        return Result.Success();
    }
}
