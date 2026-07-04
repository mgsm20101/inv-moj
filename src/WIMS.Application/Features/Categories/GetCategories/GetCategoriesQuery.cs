using MediatR;
using Microsoft.EntityFrameworkCore;
using WIMS.Application.Common.Interfaces;
using WIMS.Application.Common.Messaging;

namespace WIMS.Application.Features.Categories.GetCategories;

public sealed record CategoryDto(
    Guid Id, string Code, string NameAr, string? NameEn,
    Guid? ParentId, byte Level, string Path, int SortOrder, bool IsActive, bool IsLeaf);

public sealed record GetCategoriesQuery : IQuery<IReadOnlyList<CategoryDto>>;

public sealed class GetCategoriesQueryHandler(IAppDbContext db)
    : IRequestHandler<GetCategoriesQuery, IReadOnlyList<CategoryDto>>
{
    public async Task<IReadOnlyList<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        var categories = await db.ItemCategories.AsNoTracking()
            .OrderBy(c => c.Path)
            .Select(c => new
            {
                c.Id, c.Code, c.NameAr, c.NameEn, c.ParentId, c.Level, c.Path, c.SortOrder, c.IsActive,
                IsLeaf = !db.ItemCategories.Any(child => child.ParentId == c.Id),
            })
            .ToListAsync(cancellationToken);

        return categories
            .Select(c => new CategoryDto(c.Id, c.Code, c.NameAr, c.NameEn, c.ParentId, c.Level, c.Path, c.SortOrder, c.IsActive, c.IsLeaf))
            .ToList();
    }
}
