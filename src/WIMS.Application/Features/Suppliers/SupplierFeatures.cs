using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using WIMS.Application.Common.Interfaces;
using WIMS.Application.Common.Messaging;
using WIMS.Domain.Suppliers;
using WIMS.Shared.Results;

namespace WIMS.Application.Features.Suppliers;

public sealed record SupplierDto(Guid Id, string Code, string NameAr, string? TaxNumber, string? Phone, bool IsActive);

// ─────────────────────── إنشاء مورّد ───────────────────────
public sealed record CreateSupplierCommand : ICommand<Result<Guid>>
{
    public string Code { get; init; } = string.Empty;
    public string NameAr { get; init; } = string.Empty;
    public string? NameEn { get; init; }
    public string? TaxNumber { get; init; }
    public string? CommercialReg { get; init; }
    public string? ContactPerson { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
}

public sealed class CreateSupplierValidator : AbstractValidator<CreateSupplierCommand>
{
    public CreateSupplierValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage("كود المورّد مطلوب.").MaximumLength(20);
        RuleFor(x => x.NameAr).NotEmpty().WithMessage("اسم المورّد مطلوب.").MaximumLength(200);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("صيغة البريد غير صحيحة.");
    }
}

public sealed class CreateSupplierHandler(IAppDbContext db)
    : IRequestHandler<CreateSupplierCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateSupplierCommand request, CancellationToken ct)
    {
        var code = request.Code.Trim();
        if (await db.Suppliers.AnyAsync(s => s.Code == code, ct))
            return Error.Conflict("Supplier.Code", $"كود المورّد '{code}' مستخدم مسبقاً.");

        var supplier = new Supplier
        {
            Code = code,
            NameAr = request.NameAr.Trim(),
            NameEn = request.NameEn?.Trim(),
            TaxNumber = request.TaxNumber?.Trim(),
            CommercialReg = request.CommercialReg?.Trim(),
            ContactPerson = request.ContactPerson?.Trim(),
            Phone = request.Phone?.Trim(),
            Email = request.Email?.Trim(),
            Address = request.Address?.Trim(),
            IsActive = true,
        };
        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync(ct);
        return supplier.Id;
    }
}

// ─────────────────────── قائمة المورّدين ───────────────────────
public sealed record GetSuppliersQuery : IQuery<IReadOnlyList<SupplierDto>>;

public sealed class GetSuppliersHandler(IAppDbContext db)
    : IRequestHandler<GetSuppliersQuery, IReadOnlyList<SupplierDto>>
{
    public async Task<IReadOnlyList<SupplierDto>> Handle(GetSuppliersQuery request, CancellationToken ct)
        => await db.Suppliers.AsNoTracking()
            .OrderBy(s => s.Code)
            .Select(s => new SupplierDto(s.Id, s.Code, s.NameAr, s.TaxNumber, s.Phone, s.IsActive))
            .ToListAsync(ct);
}
