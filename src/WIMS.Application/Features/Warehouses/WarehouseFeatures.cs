using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using WIMS.Application.Common.Interfaces;
using WIMS.Application.Common.Messaging;
using WIMS.Domain.Enums;
using WIMS.Domain.Warehousing;
using WIMS.Shared.Results;

namespace WIMS.Application.Features.Warehouses;

public sealed record WarehouseDto(
    Guid Id, string Code, string NameAr, WarehouseType WarehouseType,
    WarehouseStatus Status, string? Region, bool UsesLocations, bool IsActive);

// ── إنشاء مخزن ──
public sealed record CreateWarehouseCommand : ICommand<Result<Guid>>
{
    public string Code { get; init; } = string.Empty;
    public string NameAr { get; init; } = string.Empty;
    public WarehouseType WarehouseType { get; init; } = WarehouseType.Main;
    public string? Region { get; init; }
    public Guid KeeperUserId { get; init; }
    public bool UsesLocations { get; init; }
}

public sealed class CreateWarehouseCommandValidator : AbstractValidator<CreateWarehouseCommand>
{
    public CreateWarehouseCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage("كود المخزن مطلوب.").MaximumLength(10);
        RuleFor(x => x.NameAr).NotEmpty().WithMessage("اسم المخزن مطلوب.").MaximumLength(150);
        RuleFor(x => x.KeeperUserId).NotEmpty().WithMessage("أمين المخزن مطلوب.");
        RuleFor(x => x.WarehouseType).IsInEnum();
    }
}

public sealed class CreateWarehouseCommandHandler(IAppDbContext db)
    : IRequestHandler<CreateWarehouseCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateWarehouseCommand request, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        if (await db.Warehouses.AnyAsync(w => w.Code == code, cancellationToken))
            return Error.Conflict("Warehouse.Code", $"كود المخزن '{code}' مستخدم مسبقاً.");

        var warehouse = new Warehouse
        {
            Code = code,
            NameAr = request.NameAr.Trim(),
            WarehouseType = request.WarehouseType,
            Region = request.Region?.Trim(),
            KeeperUserId = request.KeeperUserId,
            UsesLocations = request.UsesLocations,
            Status = WarehouseStatus.Active,
            IsActive = true,
        };
        db.Warehouses.Add(warehouse);
        await db.SaveChangesAsync(cancellationToken);
        return warehouse.Id;
    }
}

// ── قائمة المخازن ──
public sealed record GetWarehousesQuery : IQuery<IReadOnlyList<WarehouseDto>>;

public sealed class GetWarehousesQueryHandler(IAppDbContext db)
    : IRequestHandler<GetWarehousesQuery, IReadOnlyList<WarehouseDto>>
{
    public async Task<IReadOnlyList<WarehouseDto>> Handle(GetWarehousesQuery request, CancellationToken cancellationToken)
        => await db.Warehouses.AsNoTracking()
            .OrderBy(w => w.Code)
            .Select(w => new WarehouseDto(
                w.Id, w.Code, w.NameAr, w.WarehouseType, w.Status, w.Region, w.UsesLocations, w.IsActive))
            .ToListAsync(cancellationToken);
}
