namespace WIMS.Application.Common.Models;

/// <summary>نتيجة مصفّحة (Paged) للاستعلامات التي تُرجع قوائم.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
