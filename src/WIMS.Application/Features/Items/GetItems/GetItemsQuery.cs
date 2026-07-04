using MediatR;
using Microsoft.EntityFrameworkCore;
using WIMS.Application.Common.Interfaces;
using WIMS.Application.Common.Messaging;
using WIMS.Application.Common.Models;
using WIMS.Application.Features.Items.Dtos;

namespace WIMS.Application.Features.Items.GetItems;

/// <summary>استعلام مصفّح للأصناف مع بحث نصّي وفلترة بالتصنيف/النوع/الحالة.</summary>
public sealed record GetItemsQuery(
    string? Search = null,
    Guid? CategoryId = null,
    bool? IsActive = null,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<ItemDto>>;

public sealed class GetItemsQueryHandler(IAppDbContext db)
    : IRequestHandler<GetItemsQuery, PagedResult<ItemDto>>
{
    public async Task<PagedResult<ItemDto>> Handle(GetItemsQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var size = request.PageSize is < 1 or > 200 ? 20 : request.PageSize;

        var query = db.Items.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(i =>
                i.ItemCode.Contains(term) ||
                i.NameAr.Contains(term) ||
                (i.Barcode != null && i.Barcode.Contains(term)));
        }

        if (request.CategoryId.HasValue)
            query = query.Where(i => i.CategoryId == request.CategoryId.Value);

        if (request.IsActive.HasValue)
            query = query.Where(i => i.IsActive == request.IsActive.Value);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(i => i.ItemCode)
            .Skip((page - 1) * size)
            .Take(size)
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
            .ToListAsync(cancellationToken);

        return new PagedResult<ItemDto>(items, total, page, size);
    }
}
