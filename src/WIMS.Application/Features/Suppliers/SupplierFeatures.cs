using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using WIMS.Application.Common.Interfaces;
using WIMS.Application.Common.Messaging;
using WIMS.Domain.Suppliers;
using WIMS.Shared.Results;

namespace WIMS.Application.Features.Suppliers;

public sealed record SupplierDto(Guid Id, string Code, string NameAr, string? TaxNumber, string? Phone, bool IsActive);

// تفاصيل كاملة لتعبئة نموذج التعديل.
public sealed record SupplierDetailDto(
    Guid Id, string Code, string NameAr, string? NameEn, string? TaxNumber, string? CommercialReg,
    string? ContactPerson, string? Phone, string? Email, string? Address, bool IsActive);

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

// ─────────────────────── تفاصيل مورّد واحد (للتعديل) ───────────────────────
public sealed record GetSupplierByIdQuery(Guid Id) : IQuery<Result<SupplierDetailDto>>;

public sealed class GetSupplierByIdHandler(IAppDbContext db)
    : IRequestHandler<GetSupplierByIdQuery, Result<SupplierDetailDto>>
{
    public async Task<Result<SupplierDetailDto>> Handle(GetSupplierByIdQuery request, CancellationToken ct)
    {
        var dto = await db.Suppliers.AsNoTracking()
            .Where(s => s.Id == request.Id)
            .Select(s => new SupplierDetailDto(
                s.Id, s.Code, s.NameAr, s.NameEn, s.TaxNumber, s.CommercialReg,
                s.ContactPerson, s.Phone, s.Email, s.Address, s.IsActive))
            .FirstOrDefaultAsync(ct);

        if (dto is null) return Error.NotFound("Supplier", "المورّد غير موجود.");
        return dto;
    }
}

// ─────────────────────── تعديل مورّد ─────────────────────── (الكود ثابت)
public sealed record UpdateSupplierCommand : ICommand<Result>
{
    public Guid Id { get; init; }
    public string NameAr { get; init; } = string.Empty;
    public string? NameEn { get; init; }
    public string? TaxNumber { get; init; }
    public string? CommercialReg { get; init; }
    public string? ContactPerson { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public bool IsActive { get; init; }
}

public sealed class UpdateSupplierValidator : AbstractValidator<UpdateSupplierCommand>
{
    public UpdateSupplierValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.NameAr).NotEmpty().WithMessage("اسم المورّد مطلوب.").MaximumLength(200);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("صيغة البريد غير صحيحة.");
    }
}

public sealed class UpdateSupplierHandler(IAppDbContext db)
    : IRequestHandler<UpdateSupplierCommand, Result>
{
    public async Task<Result> Handle(UpdateSupplierCommand request, CancellationToken ct)
    {
        var s = await db.Suppliers.FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (s is null) return Result.Failure(Error.NotFound("Supplier", "المورّد غير موجود."));

        s.NameAr = request.NameAr.Trim();
        s.NameEn = request.NameEn?.Trim();
        s.TaxNumber = request.TaxNumber?.Trim();
        s.CommercialReg = request.CommercialReg?.Trim();
        s.ContactPerson = request.ContactPerson?.Trim();
        s.Phone = request.Phone?.Trim();
        s.Email = request.Email?.Trim();
        s.Address = request.Address?.Trim();
        s.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
