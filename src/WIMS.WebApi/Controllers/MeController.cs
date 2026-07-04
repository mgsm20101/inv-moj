using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WIMS.Application.Common.Interfaces;
using WIMS.Application.Features.Admin.Account;
using WIMS.Application.Features.Admin.Users;
using WIMS.Domain.Authorization;
using WIMS.WebApi.Common;

namespace WIMS.WebApi.Controllers;

/// <summary>هوية المستخدم الحالي وخدماته الذاتية (تغيير كلمة المرور).</summary>
[ApiController]
[Route("api/me")]
[Authorize]
public sealed class MeController(ICurrentUser currentUser, ISender sender) : ControllerBase
{
    /// <summary>يعيد هوية المستخدم الحالي — endpoint محمي يتطلّب رمزاً صالحاً.</summary>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var hasPhoto = Guid.TryParse(currentUser.UserId, out var uid)
            && await sender.Send(new UserHasPhotoQuery(uid), ct);
        return Ok(new
        {
            currentUser.UserId,
            currentUser.UserName,
            currentUser.IsAuthenticated,
            hasPhoto,
        });
    }

    /// <summary>صورة المستخدم الحالي (خدمة ذاتية — بلا صلاحية إضافية).</summary>
    [HttpGet("photo")]
    public async Task<IActionResult> GetMyPhoto(CancellationToken ct)
    {
        if (!Guid.TryParse(currentUser.UserId, out var uid)) return NotFound();
        var photo = await sender.Send(new GetUserPhotoQuery(uid), ct);
        return photo is null ? NotFound() : File(photo.Data, photo.ContentType);
    }

    /// <summary>يغيّر المستخدم الحالي صورته الشخصية (خدمة ذاتية — لا تتطلّب Users.Manage).</summary>
    [HttpPost("photo")]
    [RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<IActionResult> UploadMyPhoto(IFormFile file, CancellationToken ct)
    {
        if (!Guid.TryParse(currentUser.UserId, out var uid)) return Unauthorized();
        if (file is null || file.Length == 0) return BadRequest(new { message = "الملف مطلوب." });

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        return (await sender.Send(new UploadUserPhotoCommand(uid, ms.ToArray(), file.ContentType), ct))
            .ToActionResult();
    }

    /// <summary>مثال endpoint يتطلّب صلاحية دقيقة (RBAC).</summary>
    [HttpGet("users-view")]
    [Authorize(Policy = PermissionKeys.Users.View)]
    public IActionResult UsersView() => Ok(new { message = "لديك صلاحية عرض المستخدمين." });

    /// <summary>يغيّر المستخدم الحالي كلمة مروره (خدمة ذاتية لأي مستخدم مصادَق).</summary>
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangeMyPasswordCommand command, CancellationToken ct)
        => (await sender.Send(command, ct)).ToActionResult();
}
