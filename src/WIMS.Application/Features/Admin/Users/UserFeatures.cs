using FluentValidation;
using MediatR;
using WIMS.Application.Common.Interfaces;
using WIMS.Application.Common.Messaging;
using WIMS.Shared.Results;

namespace WIMS.Application.Features.Admin.Users;

// ─────────────────────── الاستعلامات ───────────────────────

public sealed record GetUsersQuery : IQuery<IReadOnlyList<UserSummaryDto>>;

public sealed class GetUsersQueryHandler(IIdentityAdminService admin)
    : IRequestHandler<GetUsersQuery, IReadOnlyList<UserSummaryDto>>
{
    public Task<IReadOnlyList<UserSummaryDto>> Handle(GetUsersQuery request, CancellationToken ct)
        => admin.GetUsersAsync(ct);
}

public sealed record GetUserByIdQuery(Guid Id) : IQuery<Result<UserDetailDto>>;

public sealed class GetUserByIdQueryHandler(IIdentityAdminService admin)
    : IRequestHandler<GetUserByIdQuery, Result<UserDetailDto>>
{
    public Task<Result<UserDetailDto>> Handle(GetUserByIdQuery request, CancellationToken ct)
        => admin.GetUserAsync(request.Id, ct);
}

// ─────────────────────── إنشاء مستخدم ───────────────────────

public sealed record CreateUserCommand : ICommand<Result<Guid>>
{
    public string UserName { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string Password { get; init; } = string.Empty;
    public IReadOnlyList<Guid> RoleIds { get; init; } = [];
}

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.UserName).NotEmpty().WithMessage("اسم المستخدم مطلوب.").MaximumLength(64);
        RuleFor(x => x.FullName).NotEmpty().WithMessage("الاسم الكامل مطلوب.").MaximumLength(150);
        RuleFor(x => x.Password).NotEmpty().WithMessage("كلمة المرور مطلوبة.").MinimumLength(8)
            .WithMessage("كلمة المرور يجب ألا تقل عن 8 أحرف.");
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("البريد الإلكتروني غير صالح.");
    }
}

public sealed class CreateUserCommandHandler(IIdentityAdminService admin)
    : IRequestHandler<CreateUserCommand, Result<Guid>>
{
    public Task<Result<Guid>> Handle(CreateUserCommand request, CancellationToken ct)
        => admin.CreateUserAsync(
            request.UserName.Trim(), request.FullName.Trim(), request.Email?.Trim(),
            request.Password, request.RoleIds, ct);
}

// ─────────────────────── تعديل بيانات مستخدم ───────────────────────

public sealed record UpdateUserCommand : ICommand<Result>
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string? Email { get; init; }
}

public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.FullName).NotEmpty().WithMessage("الاسم الكامل مطلوب.").MaximumLength(150);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("البريد الإلكتروني غير صالح.");
    }
}

public sealed class UpdateUserCommandHandler(IIdentityAdminService admin)
    : IRequestHandler<UpdateUserCommand, Result>
{
    public Task<Result> Handle(UpdateUserCommand request, CancellationToken ct)
        => admin.UpdateUserAsync(request.Id, request.FullName.Trim(), request.Email?.Trim(), ct);
}

// ─────────────────────── تفعيل/تعطيل مستخدم ───────────────────────

public sealed record SetUserActiveCommand(Guid Id, bool IsActive) : ICommand<Result>;

public sealed class SetUserActiveCommandHandler(IIdentityAdminService admin, ICurrentUser current)
    : IRequestHandler<SetUserActiveCommand, Result>
{
    public Task<Result> Handle(SetUserActiveCommand request, CancellationToken ct)
    {
        // منع المستخدم من تعطيل نفسه (يُغلق على نفسه خارج النظام).
        if (!request.IsActive && string.Equals(current.UserId, request.Id.ToString(), StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(Result.Failure(Error.Forbidden("User.Self", "لا يمكنك تعطيل حسابك الخاص.")));

        return admin.SetUserActiveAsync(request.Id, request.IsActive, ct);
    }
}

// ─────────────────────── إعادة تعيين كلمة المرور (بواسطة المدير) ───────────────────────

public sealed record ResetUserPasswordCommand(Guid Id, string NewPassword) : ICommand<Result>;

public sealed class ResetUserPasswordCommandValidator : AbstractValidator<ResetUserPasswordCommand>
{
    public ResetUserPasswordCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().WithMessage("كلمة المرور الجديدة مطلوبة.")
            .MinimumLength(8).WithMessage("كلمة المرور يجب ألا تقل عن 8 أحرف.");
    }
}

public sealed class ResetUserPasswordCommandHandler(IIdentityAdminService admin)
    : IRequestHandler<ResetUserPasswordCommand, Result>
{
    public Task<Result> Handle(ResetUserPasswordCommand request, CancellationToken ct)
        => admin.ResetPasswordAsync(request.Id, request.NewPassword, ct);
}

// ─────────────────────── إسناد أدوار المستخدم ───────────────────────

public sealed record AssignUserRolesCommand(Guid Id, IReadOnlyList<Guid> RoleIds) : ICommand<Result>;

public sealed class AssignUserRolesCommandHandler(IIdentityAdminService admin, ICurrentUser current)
    : IRequestHandler<AssignUserRolesCommand, Result>
{
    public async Task<Result> Handle(AssignUserRolesCommand request, CancellationToken ct)
    {
        // منع المستخدم من *تغيير* أدوار نفسه (تفادياً لإسقاط صلاحياته أو تصعيدها سهواً).
        // لكن حفظ الملف الشخصي دون تغيير الأدوار مسموح (no-op) حتى لا يُعطَّل تعديل الاسم/الصورة.
        if (string.Equals(current.UserId, request.Id.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            var detail = await admin.GetUserAsync(request.Id, ct);
            var currentRoles = detail.IsSuccess ? detail.Value.RoleIds.ToHashSet() : [];
            var targetRoles = (request.RoleIds ?? []).ToHashSet();
            if (!currentRoles.SetEquals(targetRoles))
                return Result.Failure(Error.Forbidden("User.Self", "لا يمكنك تعديل أدوار حسابك الخاص."));
            return Result.Success();
        }

        return await admin.SetUserRolesAsync(request.Id, request.RoleIds ?? [], ct);
    }
}

// ─────────────────────── رفع صورة المستخدم ───────────────────────

public sealed record UploadUserPhotoCommand(Guid Id, byte[] Data, string ContentType) : ICommand<Result>;

public sealed class UploadUserPhotoCommandValidator : AbstractValidator<UploadUserPhotoCommand>
{
    private static readonly string[] AllowedTypes = ["image/jpeg", "image/png", "image/webp"];
    private const int MaxBytes = 2 * 1024 * 1024; // 2MB

    public UploadUserPhotoCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Data).NotEmpty().WithMessage("الصورة مطلوبة.")
            .Must(d => d.Length <= MaxBytes).WithMessage("حجم الصورة يجب ألا يتجاوز 2 ميجابايت.");
        RuleFor(x => x.ContentType).Must(t => AllowedTypes.Contains(t))
            .WithMessage("صيغة الصورة غير مدعومة (المسموح: JPEG أو PNG أو WebP).");
    }
}

public sealed class UploadUserPhotoCommandHandler(IIdentityAdminService admin)
    : IRequestHandler<UploadUserPhotoCommand, Result>
{
    public Task<Result> Handle(UploadUserPhotoCommand request, CancellationToken ct)
        => admin.SetUserPhotoAsync(request.Id, request.Data, request.ContentType, ct);
}

// ─────────────────────── جلب صورة المستخدم (استعلام، غير مُدقَّق) ───────────────────────

public sealed record GetUserPhotoQuery(Guid Id) : IQuery<UserPhotoDto?>;

public sealed class GetUserPhotoQueryHandler(IIdentityAdminService admin)
    : IRequestHandler<GetUserPhotoQuery, UserPhotoDto?>
{
    public Task<UserPhotoDto?> Handle(GetUserPhotoQuery request, CancellationToken ct)
        => admin.GetUserPhotoAsync(request.Id, ct);
}

public sealed record UserHasPhotoQuery(Guid Id) : IQuery<bool>;

public sealed class UserHasPhotoQueryHandler(IIdentityAdminService admin)
    : IRequestHandler<UserHasPhotoQuery, bool>
{
    public Task<bool> Handle(UserHasPhotoQuery request, CancellationToken ct)
        => admin.UserHasPhotoAsync(request.Id, ct);
}
