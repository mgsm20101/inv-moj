using WIMS.Application.Common.Messaging;
using WIMS.Domain.Enums;
using WIMS.Shared.Results;

namespace WIMS.Application.Features.Transactions.CreateVoucher;

/// <summary>سطر مُدخَل ضمن أمر إنشاء سند.</summary>
public sealed record VoucherLineInput
{
    public Guid ItemId { get; init; }
    public Guid? LocationId { get; init; }
    public Guid? ToLocationId { get; init; }
    public decimal Qty { get; init; }
    public decimal? QtyAccepted { get; init; }
    public decimal? QtyRejected { get; init; }
    public string? RejectReason { get; init; }
    public string? BatchNo { get; init; }
    public string? SerialNo { get; init; }
    public DateOnly? ExpiryDate { get; init; }
    public decimal? UnitCost { get; init; }
    public string? Notes { get; init; }
}

/// <summary>ينشئ سند حركة في حالة مسودة (Draft). التحقق حسب نوع السند في الـ Handler.</summary>
public sealed record CreateVoucherCommand : ICommand<Result<Guid>>
{
    public VoucherType VoucherType { get; init; }
    public Guid WarehouseId { get; init; }
    public Guid? ToWarehouseId { get; init; }
    public Guid? SupplierId { get; init; }
    public Guid? SourceVoucherId { get; init; }
    public string? ReferenceNo { get; init; }
    public string? CostCenter { get; init; }
    public string? RequestingDept { get; init; }
    public string? Reason { get; init; }
    public Guid? RecipientEmployeeId { get; init; }
    public AdjustmentType? AdjustmentType { get; init; }
    public string? Notes { get; init; }
    public List<VoucherLineInput> Lines { get; init; } = [];
}
