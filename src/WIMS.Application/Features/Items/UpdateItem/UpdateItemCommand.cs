using WIMS.Application.Common.Messaging;
using WIMS.Domain.Enums;
using WIMS.Shared.Results;

namespace WIMS.Application.Features.Items.UpdateItem;

/// <summary>يحدّث بيانات صنف قائم (لا يغيّر الكود أو التصنيف أو النوع).</summary>
public sealed record UpdateItemCommand : ICommand<Result>
{
    public Guid Id { get; init; }
    public string NameAr { get; init; } = string.Empty;
    public string? NameEn { get; init; }
    public string? Description { get; init; }
    public string? Barcode { get; init; }

    public decimal MinStock { get; init; }
    public decimal? MaxStock { get; init; }
    public decimal ReorderPoint { get; init; }
    public decimal? ReorderQty { get; init; }

    public string? HazardClass { get; init; }
    public string? StorageConditions { get; init; }
    public int? ShelfLifeDays { get; init; }
}
