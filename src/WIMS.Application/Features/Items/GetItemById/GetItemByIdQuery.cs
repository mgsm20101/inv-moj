using MediatR;
using Microsoft.EntityFrameworkCore;
using WIMS.Application.Common.Interfaces;
using WIMS.Application.Common.Messaging;
using WIMS.Application.Features.Items.Dtos;
using WIMS.Shared.Results;

namespace WIMS.Application.Features.Items.GetItemById;

public sealed record GetItemByIdQuery(Guid Id) : IQuery<Result<ItemDto>>;

public sealed class GetItemByIdQueryHandler(IAppDbContext db)
    : IRequestHandler<GetItemByIdQuery, Result<ItemDto>>
{
    public async Task<Result<ItemDto>> Handle(GetItemByIdQuery request, CancellationToken cancellationToken)
    {
        var dto = await db.Items.AsNoTracking()
            .Where(i => i.Id == request.Id)
            .Select(i => new ItemDto
            {
                Id = i.Id,
                ItemCode = i.ItemCode,
                Barcode = i.Barcode,
                NameAr = i.NameAr,
                NameEn = i.NameEn,
                Description = i.Description,
                CategoryId = i.CategoryId,
                CategoryName = i.Category.NameAr,
                ItemType = i.ItemType,
                BaseUnitId = i.BaseUnitId,
                BaseUnitName = i.BaseUnit.NameAr,
                TracksBatch = i.TracksBatch,
                TracksExpiry = i.TracksExpiry,
                TracksSerial = i.TracksSerial,
                MinStock = i.MinStock,
                MaxStock = i.MaxStock,
                ReorderPoint = i.ReorderPoint,
                WeightedAvgCost = i.WeightedAvgCost,
                HazardClass = i.HazardClass,
                IsActive = i.IsActive,
                IsStockItem = i.IsStockItem,
            })
            .FirstOrDefaultAsync(cancellationToken);

        return dto is null
            ? Error.NotFound("Item.NotFound", "الصنف غير موجود.")
            : dto;
    }
}
