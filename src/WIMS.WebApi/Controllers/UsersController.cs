using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WIMS.Application.Features.Admin.Users;
using WIMS.Domain.Authorization;
using WIMS.WebApi.Common;

namespace WIMS.WebApi.Controllers;

/// <summary>إدارة المستخدمين — إنشاء، تعديل، تفعيل/تعطيل، إعادة تعيين كلمة المرور، وإسناد الأدوار.</summary>
[ApiController]
[Route("api/users")]
[Authorize]
public sealed class UsersController(ISender sender) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionKeys.Users.View)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(await sender.Send(new GetUsersQuery(), ct));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = PermissionKeys.Users.View)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => (await sender.Send(new GetUserByIdQuery(id), ct)).ToActionResult();

    [HttpPost]
    [Authorize(Policy = PermissionKeys.Users.Manage)]
    public async Task<IActionResult> Create([FromBody] CreateUserCommand command, CancellationToken ct)
        => (await sender.Send(command, ct)).ToActionResult();

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PermissionKeys.Users.Manage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest body, CancellationToken ct)
        => (await sender.Send(new UpdateUserCommand { Id = id, FullName = body.FullName, Email = body.Email }, ct))
            .ToActionResult();

    [HttpPut("{id:guid}/active")]
    [Authorize(Policy = PermissionKeys.Users.Manage)]
    public async Task<IActionResult> SetActive(Guid id, [FromBody] SetActiveRequest body, CancellationToken ct)
        => (await sender.Send(new SetUserActiveCommand(id, body.IsActive), ct)).ToActionResult();

    [HttpPost("{id:guid}/password")]
    [Authorize(Policy = PermissionKeys.Users.Manage)]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordRequest body, CancellationToken ct)
        => (await sender.Send(new ResetUserPasswordCommand(id, body.NewPassword), ct)).ToActionResult();

    [HttpPut("{id:guid}/roles")]
    [Authorize(Policy = PermissionKeys.Users.Manage)]
    public async Task<IActionResult> AssignRoles(Guid id, [FromBody] AssignRolesRequest body, CancellationToken ct)
        => (await sender.Send(new AssignUserRolesCommand(id, body.RoleIds ?? []), ct)).ToActionResult();
}

public sealed record UpdateUserRequest(string FullName, string? Email);
public sealed record SetActiveRequest(bool IsActive);
public sealed record ResetPasswordRequest(string NewPassword);
public sealed record AssignRolesRequest(IReadOnlyList<Guid> RoleIds);
