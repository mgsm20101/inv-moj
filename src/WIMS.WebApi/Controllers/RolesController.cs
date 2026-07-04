using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WIMS.Application.Features.Admin.Roles;
using WIMS.Domain.Authorization;
using WIMS.WebApi.Common;

namespace WIMS.WebApi.Controllers;

/// <summary>إدارة الأدوار وإسناد الصلاحيات الدقيقة إليها.</summary>
[ApiController]
[Route("api/roles")]
[Authorize]
public sealed class RolesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionKeys.Roles.View)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(await sender.Send(new GetRolesQuery(), ct));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = PermissionKeys.Roles.View)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => (await sender.Send(new GetRoleByIdQuery(id), ct)).ToActionResult();

    [HttpPost]
    [Authorize(Policy = PermissionKeys.Roles.Manage)]
    public async Task<IActionResult> Create([FromBody] CreateRoleCommand command, CancellationToken ct)
        => (await sender.Send(command, ct)).ToActionResult();

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PermissionKeys.Roles.Manage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRoleRequest body, CancellationToken ct)
        => (await sender.Send(new UpdateRoleCommand { Id = id, Name = body.Name, Description = body.Description }, ct))
            .ToActionResult();

    [HttpPut("{id:guid}/permissions")]
    [Authorize(Policy = PermissionKeys.Roles.Manage)]
    public async Task<IActionResult> SetPermissions(Guid id, [FromBody] SetPermissionsRequest body, CancellationToken ct)
        => (await sender.Send(new SetRolePermissionsCommand(id, body.PermissionKeys ?? []), ct)).ToActionResult();

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = PermissionKeys.Roles.Manage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        => (await sender.Send(new DeleteRoleCommand(id), ct)).ToActionResult();
}

public sealed record UpdateRoleRequest(string Name, string? Description);
public sealed record SetPermissionsRequest(IReadOnlyList<string> PermissionKeys);
