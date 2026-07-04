using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using WIMS.Application.Common.Interfaces;
using WIMS.Application.Common.Messaging;
using WIMS.Domain.Employees;
using WIMS.Domain.Enums;
using WIMS.Shared.Results;

namespace WIMS.Application.Features.Employees;

public sealed record EmployeeDto(
    Guid Id, string EmployeeNo, string NationalId, string FullNameAr,
    string Department, string CostCenter, string? Email, EmployeeStatus Status);

// تفاصيل كاملة (تشمل الحقول غير المعروضة في القائمة) لتعبئة نموذج التعديل.
public sealed record EmployeeDetailDto(
    Guid Id, string EmployeeNo, string NationalId, string FullNameAr, string? FullNameEn,
    string Department, string? JobTitle, string CostCenter, string? Email, string? Phone,
    Guid? UserId, EmployeeStatus Status);

// ─────────────────────── إنشاء موظف ───────────────────────
public sealed record CreateEmployeeCommand : ICommand<Result<Guid>>
{
    public string EmployeeNo { get; init; } = string.Empty;
    public string NationalId { get; init; } = string.Empty;
    public string FullNameAr { get; init; } = string.Empty;
    public string? FullNameEn { get; init; }
    public string Department { get; init; } = string.Empty;
    public string? JobTitle { get; init; }
    public string CostCenter { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public Guid? UserId { get; init; }
}

public sealed class CreateEmployeeValidator : AbstractValidator<CreateEmployeeCommand>
{
    public CreateEmployeeValidator()
    {
        RuleFor(x => x.EmployeeNo).NotEmpty().WithMessage("الرقم الوظيفي مطلوب.").MaximumLength(20);
        RuleFor(x => x.NationalId).NotEmpty().Matches(@"^\d{10}$").WithMessage("الهوية الوطنية يجب أن تكون 10 أرقام.");
        RuleFor(x => x.FullNameAr).NotEmpty().WithMessage("اسم الموظف مطلوب.").MaximumLength(150);
        RuleFor(x => x.Department).NotEmpty().WithMessage("الإدارة مطلوبة.").MaximumLength(120);
        RuleFor(x => x.CostCenter).NotEmpty().WithMessage("مركز التكلفة مطلوب.").MaximumLength(30);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("صيغة البريد غير صحيحة.");
    }
}

public sealed class CreateEmployeeHandler(IAppDbContext db) : IRequestHandler<CreateEmployeeCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateEmployeeCommand request, CancellationToken ct)
    {
        var no = request.EmployeeNo.Trim();
        var nid = request.NationalId.Trim();
        if (await db.Employees.AnyAsync(e => e.EmployeeNo == no, ct))
            return Error.Conflict("Employee.No", $"الرقم الوظيفي '{no}' مستخدم مسبقاً.");
        if (await db.Employees.AnyAsync(e => e.NationalId == nid, ct))
            return Error.Conflict("Employee.NationalId", $"الهوية '{nid}' مستخدمة مسبقاً.");

        var employee = new Employee
        {
            EmployeeNo = no, NationalId = nid,
            FullNameAr = request.FullNameAr.Trim(), FullNameEn = request.FullNameEn?.Trim(),
            Department = request.Department.Trim(), JobTitle = request.JobTitle?.Trim(),
            CostCenter = request.CostCenter.Trim(), Email = request.Email?.Trim(), Phone = request.Phone?.Trim(),
            UserId = request.UserId, Status = EmployeeStatus.Active,
        };
        db.Employees.Add(employee);
        await db.SaveChangesAsync(ct);
        return employee.Id;
    }
}

// ─────────────────────── تعديل موظف ─────────────────────── (الرقم الوظيفي والهوية ثابتان)
public sealed record UpdateEmployeeCommand : ICommand<Result>
{
    public Guid Id { get; init; }
    public string FullNameAr { get; init; } = string.Empty;
    public string? FullNameEn { get; init; }
    public string Department { get; init; } = string.Empty;
    public string? JobTitle { get; init; }
    public string CostCenter { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public Guid? UserId { get; init; }
}

public sealed class UpdateEmployeeValidator : AbstractValidator<UpdateEmployeeCommand>
{
    public UpdateEmployeeValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.FullNameAr).NotEmpty().WithMessage("اسم الموظف مطلوب.").MaximumLength(150);
        RuleFor(x => x.Department).NotEmpty().WithMessage("الإدارة مطلوبة.").MaximumLength(120);
        RuleFor(x => x.CostCenter).NotEmpty().WithMessage("مركز التكلفة مطلوب.").MaximumLength(30);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("صيغة البريد غير صحيحة.");
    }
}

public sealed class UpdateEmployeeHandler(IAppDbContext db) : IRequestHandler<UpdateEmployeeCommand, Result>
{
    public async Task<Result> Handle(UpdateEmployeeCommand request, CancellationToken ct)
    {
        var e = await db.Employees.FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (e is null) return Result.Failure(Error.NotFound("Employee", "الموظف غير موجود."));

        e.FullNameAr = request.FullNameAr.Trim();
        e.FullNameEn = request.FullNameEn?.Trim();
        e.Department = request.Department.Trim();
        e.JobTitle = request.JobTitle?.Trim();
        e.CostCenter = request.CostCenter.Trim();
        e.Email = request.Email?.Trim();
        e.Phone = request.Phone?.Trim();
        e.UserId = request.UserId;
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ─────────────────────── قائمة الموظفين ───────────────────────
public sealed record GetEmployeesQuery(string? Search = null) : IQuery<IReadOnlyList<EmployeeDto>>;

public sealed class GetEmployeesHandler(IAppDbContext db) : IRequestHandler<GetEmployeesQuery, IReadOnlyList<EmployeeDto>>
{
    public async Task<IReadOnlyList<EmployeeDto>> Handle(GetEmployeesQuery request, CancellationToken ct)
    {
        var q = db.Employees.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            q = q.Where(e => e.EmployeeNo.Contains(term) || e.FullNameAr.Contains(term) || e.NationalId.Contains(term));
        }
        return await q.OrderBy(e => e.EmployeeNo)
            .Select(e => new EmployeeDto(e.Id, e.EmployeeNo, e.NationalId, e.FullNameAr, e.Department, e.CostCenter, e.Email, e.Status))
            .ToListAsync(ct);
    }
}

// ─────────────────────── تفاصيل موظف واحد (للتعديل) ───────────────────────
public sealed record GetEmployeeByIdQuery(Guid Id) : IQuery<Result<EmployeeDetailDto>>;

public sealed class GetEmployeeByIdHandler(IAppDbContext db) : IRequestHandler<GetEmployeeByIdQuery, Result<EmployeeDetailDto>>
{
    public async Task<Result<EmployeeDetailDto>> Handle(GetEmployeeByIdQuery request, CancellationToken ct)
    {
        var dto = await db.Employees.AsNoTracking()
            .Where(e => e.Id == request.Id)
            .Select(e => new EmployeeDetailDto(
                e.Id, e.EmployeeNo, e.NationalId, e.FullNameAr, e.FullNameEn,
                e.Department, e.JobTitle, e.CostCenter, e.Email, e.Phone, e.UserId, e.Status))
            .FirstOrDefaultAsync(ct);

        if (dto is null) return Error.NotFound("Employee", "الموظف غير موجود.");
        return dto;
    }
}

// ─────────────────────── تغيير حالة موظف (BR-EMP-03) ───────────────────────
public sealed record ChangeEmployeeStatusCommand(Guid Id, EmployeeStatus Status) : ICommand<Result>;

public sealed class ChangeEmployeeStatusHandler(IAppDbContext db) : IRequestHandler<ChangeEmployeeStatusCommand, Result>
{
    public async Task<Result> Handle(ChangeEmployeeStatusCommand request, CancellationToken ct)
    {
        var e = await db.Employees.FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (e is null) return Result.Failure(Error.NotFound("Employee", "الموظف غير موجود."));

        // منع النقل/إنهاء الخدمة مع وجود عهدة قائمة.
        if (request.Status is EmployeeStatus.Terminated or EmployeeStatus.Transferred)
        {
            var openItems = await db.CustodyItems
                .CountAsync(ci => ci.Status == CustodyItemStatus.InCustody
                    && db.Custodies.Any(c => c.Id == ci.CustodyId && c.EmployeeId == e.Id), ct);
            if (openItems > 0)
                return Result.Failure(Error.Conflict("Employee.Custody",
                    $"لا يمكن نقل/إنهاء خدمة الموظف — لديه {openItems} بند عهدة قائم. يجب إخلاء الذمة أولاً."));
        }

        e.Status = request.Status;
        if (request.Status == EmployeeStatus.Terminated)
            e.TerminationDate = DateOnly.FromDateTime(DateTime.UtcNow);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
