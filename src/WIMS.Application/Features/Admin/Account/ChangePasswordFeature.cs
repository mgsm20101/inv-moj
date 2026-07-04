using FluentValidation;
using MediatR;
using WIMS.Application.Common.Interfaces;
using WIMS.Application.Common.Messaging;
using WIMS.Shared.Results;

namespace WIMS.Application.Features.Admin.Account;

/// <summary>يغيّر المستخدم الحالي كلمة مروره (خدمة ذاتية).</summary>
public sealed record ChangeMyPasswordCommand(string CurrentPassword, string NewPassword) : ICommand<Result>;

public sealed class ChangeMyPasswordCommandValidator : AbstractValidator<ChangeMyPasswordCommand>
{
    public ChangeMyPasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage("كلمة المرور الحالية مطلوبة.");
        RuleFor(x => x.NewPassword).NotEmpty().WithMessage("كلمة المرور الجديدة مطلوبة.")
            .MinimumLength(8).WithMessage("كلمة المرور يجب ألا تقل عن 8 أحرف.")
            .NotEqual(x => x.CurrentPassword).WithMessage("كلمة المرور الجديدة يجب أن تختلف عن الحالية.");
    }
}

public sealed class ChangeMyPasswordCommandHandler(IIdentityAdminService admin, ICurrentUser current)
    : IRequestHandler<ChangeMyPasswordCommand, Result>
{
    public Task<Result> Handle(ChangeMyPasswordCommand request, CancellationToken ct)
    {
        if (!Guid.TryParse(current.UserId, out var userId))
            return Task.FromResult(Result.Failure(Error.Unauthorized("Auth.User", "تعذّر تحديد المستخدم الحالي.")));

        return admin.ChangeOwnPasswordAsync(userId, request.CurrentPassword, request.NewPassword, ct);
    }
}
