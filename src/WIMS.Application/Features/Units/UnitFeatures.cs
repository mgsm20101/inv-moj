using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using WIMS.Application.Common.Interfaces;
using WIMS.Application.Common.Messaging;
using WIMS.Domain.Catalog;
using WIMS.Shared.Results;

namespace WIMS.Application.Features.Units;

public sealed record UnitDto(Guid Id, string Code, string NameAr, bool IsBaseUnit, bool IsActive);

// ── إنشاء وحدة قياس ──
public sealed record CreateUnitCommand(string Code, string NameAr, bool IsBaseUnit = true) : ICommand<Result<Guid>>;

public sealed class CreateUnitCommandValidator : AbstractValidator<CreateUnitCommand>
{
    public CreateUnitCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage("رمز الوحدة مطلوب.").MaximumLength(10);
        RuleFor(x => x.NameAr).NotEmpty().WithMessage("اسم الوحدة مطلوب.").MaximumLength(50);
    }
}

public sealed class CreateUnitCommandHandler(IAppDbContext db)
    : IRequestHandler<CreateUnitCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateUnitCommand request, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        if (await db.UnitsOfMeasure.AnyAsync(u => u.Code == code, cancellationToken))
            return Error.Conflict("Unit.Code", $"رمز الوحدة '{code}' مستخدم مسبقاً.");

        var unit = new UnitOfMeasure
        {
            Code = code,
            NameAr = request.NameAr.Trim(),
            IsBaseUnit = request.IsBaseUnit,
            IsActive = true,
        };
        db.UnitsOfMeasure.Add(unit);
        await db.SaveChangesAsync(cancellationToken);
        return unit.Id;
    }
}

// ── قائمة الوحدات ──
public sealed record GetUnitsQuery : IQuery<IReadOnlyList<UnitDto>>;

public sealed class GetUnitsQueryHandler(IAppDbContext db)
    : IRequestHandler<GetUnitsQuery, IReadOnlyList<UnitDto>>
{
    public async Task<IReadOnlyList<UnitDto>> Handle(GetUnitsQuery request, CancellationToken cancellationToken)
        => await db.UnitsOfMeasure.AsNoTracking()
            .OrderBy(u => u.Code)
            .Select(u => new UnitDto(u.Id, u.Code, u.NameAr, u.IsBaseUnit, u.IsActive))
            .ToListAsync(cancellationToken);
}
