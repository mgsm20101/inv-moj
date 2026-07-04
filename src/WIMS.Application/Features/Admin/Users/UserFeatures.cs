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
    public Task<Result> Handle(AssignUserRolesCommand request, CancellationToken ct)
    {
        // منع المستخدم من تعديل أدوار نفسه (تفادياً لإسقاط صلاحياته سهواً).
        if (string.Equals(current.UserId, request.Id.ToString(), StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(Result.Failure(Error.Forbidden("User.Self", "لا يمكنك تعديل أدوار حسابك الخاص.")));

        return admin.SetUserRolesAsync(request.Id, request.RoleIds ?? [], ct);
    }
}
