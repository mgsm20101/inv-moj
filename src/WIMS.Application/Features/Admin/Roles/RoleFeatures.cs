using FluentValidation;
using MediatR;
using WIMS.Application.Common.Interfaces;
using WIMS.Application.Common.Messaging;
using WIMS.Shared.Results;

namespace WIMS.Application.Features.Admin.Roles;

// ─────────────────────── الاستعلامات ───────────────────────

public sealed record GetRolesQuery : IQuery<IReadOnlyList<RoleSummaryDto>>;

public sealed class GetRolesQueryHandler(IIdentityAdminService admin)
    : IRequestHandler<GetRolesQuery, IReadOnlyList<RoleSummaryDto>>
{
    public Task<IReadOnlyList<RoleSummaryDto>> Handle(GetRolesQuery request, CancellationToken ct)
        => admin.GetRolesAsync(ct);
}

public sealed record GetRoleByIdQuery(Guid Id) : IQuery<Result<RoleDetailDto>>;

public sealed class GetRoleByIdQueryHandler(IIdentityAdminService admin)
    : IRequestHandler<GetRoleByIdQuery, Result<RoleDetailDto>>
{
    public Task<Result<RoleDetailDto>> Handle(GetRoleByIdQuery request, CancellationToken ct)
        => admin.GetRoleAsync(request.Id, ct);
}

// ─────────────────────── إنشاء دور ───────────────────────

public sealed record CreateRoleCommand : ICommand<Result<Guid>>
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public IReadOnlyList<string> PermissionKeys { get; init; } = [];
}

public sealed class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("اسم الدور مطلوب.").MaximumLength(128);
    }
}

public sealed class CreateRoleCommandHandler(IIdentityAdminService admin)
    : IRequestHandler<CreateRoleCommand, Result<Guid>>
{
    public Task<Result<Guid>> Handle(CreateRoleCommand request, CancellationToken ct)
        => admin.CreateRoleAsync(request.Name.Trim(), request.Description?.Trim(), request.PermissionKeys ?? [], ct);
}

// ─────────────────────── تعديل دور ───────────────────────

public sealed record UpdateRoleCommand : ICommand<Result>
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
}

public sealed class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().WithMessage("اسم الدور مطلوب.").MaximumLength(128);
    }
}

public sealed class UpdateRoleCommandHandler(IIdentityAdminService admin)
    : IRequestHandler<UpdateRoleCommand, Result>
{
    public Task<Result> Handle(UpdateRoleCommand request, CancellationToken ct)
        => admin.UpdateRoleAsync(request.Id, request.Name.Trim(), request.Description?.Trim(), ct);
}

// ─────────────────────── ضبط صلاحيات الدور ───────────────────────

public sealed record SetRolePermissionsCommand(Guid Id, IReadOnlyList<string> PermissionKeys) : ICommand<Result>;

public sealed class SetRolePermissionsCommandHandler(IIdentityAdminService admin)
    : IRequestHandler<SetRolePermissionsCommand, Result>
{
    public Task<Result> Handle(SetRolePermissionsCommand request, CancellationToken ct)
        => admin.SetRolePermissionsAsync(request.Id, request.PermissionKeys ?? [], ct);
}

// ─────────────────────── حذف دور ───────────────────────

public sealed record DeleteRoleCommand(Guid Id) : ICommand<Result>;

public sealed class DeleteRoleCommandHandler(IIdentityAdminService admin)
    : IRequestHandler<DeleteRoleCommand, Result>
{
    public Task<Result> Handle(DeleteRoleCommand request, CancellationToken ct)
        => admin.DeleteRoleAsync(request.Id, ct);
}
