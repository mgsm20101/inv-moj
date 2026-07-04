using MediatR;
using Microsoft.EntityFrameworkCore;
using WIMS.Application.Common.Interfaces;
using WIMS.Shared.Results;

namespace WIMS.Application.Features.Items.UpdateItem;

public sealed class UpdateItemCommandHandler(IAppDbContext db)
    : IRequestHandler<UpdateItemCommand, Result>
{
    public async Task<Result> Handle(UpdateItemCommand request, CancellationToken cancellationToken)
    {
        var item = await db.Items.FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken);
        if (item is null)
            return Result.Failure(Error.NotFound("Item.NotFound", "الصنف غير موجود."));

        if (!string.IsNullOrWhiteSpace(request.Barcode))
        {
            var barcode = request.Barcode.Trim();
            if (await db.Items.AnyAsync(i => i.Barcode == barcode && i.Id != item.Id, cancellationToken))
                return Result.Failure(Error.Conflict("Item.Barcode", $"الباركود '{barcode}' مستخدم مسبقاً."));
            item.Barcode = barcode;
        }
        else
        {
            item.Barcode = null;
        }

        item.NameAr = request.NameAr.Trim();
        item.NameEn = request.NameEn?.Trim();
        item.Description = request.Description?.Trim();
        item.MinStock = request.MinStock;
        item.MaxStock = request.MaxStock;
        item.ReorderPoint = request.ReorderPoint;
        item.ReorderQty = request.ReorderQty;
        item.HazardClass = request.HazardClass?.Trim();
        item.StorageConditions = request.StorageConditions?.Trim();
        item.ShelfLifeDays = request.ShelfLifeDays;

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
