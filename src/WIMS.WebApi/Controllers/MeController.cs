using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WIMS.Application.Common.Interfaces;
using WIMS.Application.Features.Admin.Account;
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
    public IActionResult Get() => Ok(new
    {
        currentUser.UserId,
        currentUser.UserName,
        currentUser.IsAuthenticated,
    });

    /// <summary>مثال endpoint يتطلّب صلاحية دقيقة (RBAC).</summary>
    [HttpGet("users-view")]
    [Authorize(Policy = PermissionKeys.Users.View)]
    public IActionResult UsersView() => Ok(new { message = "لديك صلاحية عرض المستخدمين." });

    /// <summary>يغيّر المستخدم الحالي كلمة مروره (خدمة ذاتية لأي مستخدم مصادَق).</summary>
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangeMyPasswordCommand command, CancellationToken ct)
        => (await sender.Send(command, ct)).ToActionResult();
}
