using MediatR;
using Microsoft.EntityFrameworkCore;
using WIMS.Application.Common.Interfaces;
using WIMS.Domain.Catalog;
using WIMS.Domain.Enums;
using WIMS.Shared.Results;

namespace WIMS.Application.Features.Items.CreateItem;

public sealed class CreateItemCommandHandler(
    IAppDbContext db,
    IItemCodeGenerator codeGenerator)
    : IRequestHandler<CreateItemCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateItemCommand request, CancellationToken cancellationToken)
    {
        // ── التحقق المرجعي: التصنيف موجود ونشط وعقدة ورقية (BR-09) ──
        var category = await db.ItemCategories
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken);
        if (category is null || !category.IsActive)
            return Error.NotFound("Item.Category", "التصنيف غير موجود أو غير نشط.");

        var hasChildren = await db.ItemCategories
            .AnyAsync(c => c.ParentId == request.CategoryId, cancellationToken);
        if (hasChildren)
            return Error.Validation("Item.Category", "لا يمكن ربط الصنف بتصنيف غير نهائي (يحتوي تصنيفات فرعية).");

        // ── وحدة القياس موجودة ونشطة ──
        var unitExists = await db.UnitsOfMeasure
            .AnyAsync(u => u.Id == request.BaseUnitId && u.IsActive, cancellationToken);
        if (!unitExists)
            return Error.NotFound("Item.Unit", "وحدة القياس غير معرّفة أو غير نشطة.");

        // ── أبعاد التتبّع: افتراضيات حسب النوع مع فرض القواعد (BR-04, BR-06) ──
        var tracksSerial = request.TracksSerial ?? request.ItemType == ItemType.Durable;
        var tracksExpiry = request.TracksExpiry ?? request.ItemType == ItemType.Perishable;
        var tracksBatch = request.TracksBatch
            ?? request.ItemType is ItemType.Hazardous or ItemType.Perishable;

        if (request.ItemType == ItemType.Perishable)
            tracksExpiry = true; // القابل للتلف يتتبّع الصلاحية دائماً.

        // ── كود الصنف: مُدخل يدوياً (يُتحقق تفرّده) أو مُولَّد آلياً (BR-01) ──
        string itemCode;
        if (!string.IsNullOrWhiteSpace(request.ItemCode))
        {
            var normalized = request.ItemCode.Trim();
            if (await db.Items.AnyAsync(i => i.ItemCode == normalized, cancellationToken))
                return Error.Conflict("Item.Code", $"كود الصنف '{normalized}' مستخدم مسبقاً.");
            itemCode = normalized;
        }
        else
        {
            itemCode = await codeGenerator.GenerateAsync(request.CategoryId, cancellationToken);
        }

        // ── الباركود فريد عند وجوده ──
        if (!string.IsNullOrWhiteSpace(request.Barcode))
        {
            var barcode = request.Barcode.Trim();
            if (await db.Items.AnyAsync(i => i.Barcode == barcode, cancellationToken))
                return Error.Conflict("Item.Barcode", $"الباركود '{barcode}' مستخدم مسبقاً.");
        }

        var item = new Item
        {
            ItemCode = itemCode,
            Barcode = string.IsNullOrWhiteSpace(request.Barcode) ? null : request.Barcode.Trim(),
            NameAr = request.NameAr.Trim(),
            NameEn = request.NameEn?.Trim(),
            Description = request.Description?.Trim(),
            CategoryId = request.CategoryId,
            ItemType = request.ItemType,
            BaseUnitId = request.BaseUnitId,
            TracksBatch = tracksBatch,
            TracksExpiry = tracksExpiry,
            TracksSerial = tracksSerial,
            MinStock = request.MinStock,
            MaxStock = request.MaxStock,
            ReorderPoint = request.ReorderPoint,
            ReorderQty = request.ReorderQty,
            HazardClass = request.HazardClass?.Trim(),
            StorageConditions = request.StorageConditions?.Trim(),
            ShelfLifeDays = request.ShelfLifeDays,
            IsStockItem = request.IsStockItem,
            RequiresCustody = request.RequiresCustody ?? request.ItemType == ItemType.Durable,
            IsActive = true,
        };

        db.Items.Add(item);
        await db.SaveChangesAsync(cancellationToken);

        return item.Id;
    }
}
