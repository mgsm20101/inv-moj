using WIMS.Application.Common.Messaging;
using WIMS.Domain.Enums;
using WIMS.Shared.Results;

namespace WIMS.Application.Features.Items.CreateItem;

/// <summary>ينشئ صنفاً جديداً. يُترك <see cref="ItemCode"/> فارغاً ليُولَّد آلياً (GG-CCCC-SSSS).</summary>
public sealed record CreateItemCommand : ICommand<Result<Guid>>
{
    public string? ItemCode { get; init; }
    public string? Barcode { get; init; }
    public string NameAr { get; init; } = string.Empty;
    public string? NameEn { get; init; }
    public string? Description { get; init; }

    public Guid CategoryId { get; init; }
    public ItemType ItemType { get; init; }
    public Guid BaseUnitId { get; init; }

    public bool? TracksBatch { get; init; }
    public bool? TracksExpiry { get; init; }
    public bool? TracksSerial { get; init; }

    public decimal MinStock { get; init; }
    public decimal? MaxStock { get; init; }
    public decimal ReorderPoint { get; init; }
    public decimal? ReorderQty { get; init; }

    public string? HazardClass { get; init; }
    public string? StorageConditions { get; init; }
    public int? ShelfLifeDays { get; init; }

    public bool IsStockItem { get; init; } = true;

    /// <summary>هل يُدار كعهدة (افتراضياً true للمستديم إن تُرك فارغاً).</summary>
    public bool? RequiresCustody { get; init; }
}
