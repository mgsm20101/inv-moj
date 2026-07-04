using Microsoft.EntityFrameworkCore;
using WIMS.Application.Common.Interfaces;
using WIMS.Infrastructure.Persistence;

namespace WIMS.Infrastructure.Services;

/// <summary>
/// يولّد كود الصنف "GG-CCCC-SSSS": GG من المجموعة الرئيسية (جذر المسار)،
/// CCCC من كود التصنيف الورقي، SSSS تسلسل تلقائي داخل التصنيف (القسم 4 من التصميم).
/// </summary>
public sealed class ItemCodeGenerator(AppDbContext db) : IItemCodeGenerator
{
    public async Task<string> GenerateAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var category = await db.ItemCategories
            .FirstOrDefaultAsync(c => c.Id == categoryId, cancellationToken)
            ?? throw new InvalidOperationException("التصنيف غير موجود لتوليد كود الصنف.");

        // GG: أول مقطع في المسار (كود الجذر) مقصوصاً إلى خانتين.
        var rootSegment = category.Path.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()
            ?? category.Code;
        var gg = Left(rootSegment, 2).PadLeft(2, '0');

        // CCCC: كود التصنيف الورقي إلى 4 خانات.
        var cccc = Left(category.Code, 4).PadLeft(4, '0');

        var prefix = $"{gg}-{cccc}-";

        // أعلى تسلسل حالي داخل هذا التصنيف.
        var existingCodes = await db.Items
            .Where(i => i.CategoryId == categoryId)
            .Select(i => i.ItemCode)
            .ToListAsync(cancellationToken);

        var maxSeq = existingCodes
            .Select(code => code.Split('-').LastOrDefault())
            .Select(seq => int.TryParse(seq, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        var next = (maxSeq + 1).ToString("D4");
        return $"{prefix}{next}";
    }

    private static string Left(string value, int length)
        => value.Length <= length ? value : value[..length];
}
