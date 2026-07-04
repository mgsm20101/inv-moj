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

    /// <summary>رفع/تحديث صورة المستخدم (multipart). تُخزَّن في قاعدة البيانات.</summary>
    [HttpPost("{id:guid}/photo")]
    [Authorize(Policy = PermissionKeys.Users.Manage)]
    [RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<IActionResult> UploadPhoto(Guid id, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "الملف مطلوب." });

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        return (await sender.Send(new UploadUserPhotoCommand(id, ms.ToArray(), file.ContentType), ct))
            .ToActionResult();
    }

    /// <summary>صورة المستخدم — تتطلّب صلاحية عرض المستخدمين.</summary>
    [HttpGet("{id:guid}/photo")]
    [Authorize(Policy = PermissionKeys.Users.View)]
    public async Task<IActionResult> GetPhoto(Guid id, CancellationToken ct)
    {
        var photo = await sender.Send(new GetUserPhotoQuery(id), ct);
        return photo is null ? NotFound() : File(photo.Data, photo.ContentType);
    }
}

public sealed record UpdateUserRequest(string FullName, string? Email);
public sealed record SetActiveRequest(bool IsActive);
public sealed record ResetPasswordRequest(string NewPassword);
public sealed record AssignRolesRequest(IReadOnlyList<Guid> RoleIds);
