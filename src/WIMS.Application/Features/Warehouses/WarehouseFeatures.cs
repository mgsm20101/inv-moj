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

// تفاصيل كاملة (تشمل KeeperUserId) لتعبئة نموذج التعديل — غير مضمّنة في قائمة العرض.
public sealed record WarehouseDetailDto(
    Guid Id, string Code, string NameAr, WarehouseType WarehouseType,
    string? Region, Guid KeeperUserId, bool UsesLocations, bool IsActive);

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

// ── تعديل مخزن ── (الكود غير قابل للتعديل)
public sealed record UpdateWarehouseCommand : ICommand<Result>
{
    public Guid Id { get; init; }
    public string NameAr { get; init; } = string.Empty;
    public WarehouseType WarehouseType { get; init; } = WarehouseType.Main;
    public string? Region { get; init; }
    public Guid KeeperUserId { get; init; }
    public bool UsesLocations { get; init; }
}

public sealed class UpdateWarehouseCommandValidator : AbstractValidator<UpdateWarehouseCommand>
{
    public UpdateWarehouseCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.NameAr).NotEmpty().WithMessage("اسم المخزن مطلوب.").MaximumLength(150);
        RuleFor(x => x.KeeperUserId).NotEmpty().WithMessage("أمين المخزن مطلوب.");
        RuleFor(x => x.WarehouseType).IsInEnum();
    }
}

public sealed class UpdateWarehouseCommandHandler(IAppDbContext db)
    : IRequestHandler<UpdateWarehouseCommand, Result>
{
    public async Task<Result> Handle(UpdateWarehouseCommand request, CancellationToken cancellationToken)
    {
        var warehouse = await db.Warehouses.FirstOrDefaultAsync(w => w.Id == request.Id, cancellationToken);
        if (warehouse is null)
            return Result.Failure(Error.NotFound("Warehouse", "المخزن غير موجود."));

        warehouse.NameAr = request.NameAr.Trim();
        warehouse.WarehouseType = request.WarehouseType;
        warehouse.Region = request.Region?.Trim();
        warehouse.KeeperUserId = request.KeeperUserId;
        warehouse.UsesLocations = request.UsesLocations;
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
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

// ── تفاصيل مخزن واحد (للتعديل) ──
public sealed record GetWarehouseByIdQuery(Guid Id) : IQuery<Result<WarehouseDetailDto>>;

public sealed class GetWarehouseByIdQueryHandler(IAppDbContext db)
    : IRequestHandler<GetWarehouseByIdQuery, Result<WarehouseDetailDto>>
{
    public async Task<Result<WarehouseDetailDto>> Handle(GetWarehouseByIdQuery request, CancellationToken cancellationToken)
    {
        var dto = await db.Warehouses.AsNoTracking()
            .Where(w => w.Id == request.Id)
            .Select(w => new WarehouseDetailDto(
                w.Id, w.Code, w.NameAr, w.WarehouseType, w.Region, w.KeeperUserId, w.UsesLocations, w.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

        if (dto is null)
            return Error.NotFound("Warehouse", "المخزن غير موجود.");
        return dto;
    }
}
