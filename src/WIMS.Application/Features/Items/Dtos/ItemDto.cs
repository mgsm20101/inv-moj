using WIMS.Domain.Enums;

namespace WIMS.Application.Features.Items.Dtos;

/// <summary>تمثيل مقروء للصنف في الاستعلامات.</summary>
public sealed record ItemDto
{
    public Guid Id { get; init; }
    public string ItemCode { get; init; } = string.Empty;
    public string? Barcode { get; init; }
    public string NameAr { get; init; } = string.Empty;
    public string? NameEn { get; init; }
    public string? Description { get; init; }

    public Guid CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;

    public ItemType ItemType { get; init; }

    public Guid BaseUnitId { get; init; }
    public string BaseUnitName { get; init; } = string.Empty;

    public bool TracksBatch { get; init; }
    public bool TracksExpiry { get; init; }
    public bool TracksSerial { get; init; }

    public decimal MinStock { get; init; }
    public decimal? MaxStock { get; init; }
    public decimal ReorderPoint { get; init; }
    public decimal WeightedAvgCost { get; init; }

    public string? HazardClass { get; init; }
    public bool IsActive { get; init; }
    public bool IsStockItem { get; init; }
}
