using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WIMS.Application.Features.Admin.Permissions;
using WIMS.Domain.Authorization;

namespace WIMS.WebApi.Controllers;

/// <summary>كتالوج الصلاحيات المعرّفة — يغذّي مصفوفة اختيار صلاحيات الدور في الواجهة.</summary>
[ApiController]
[Route("api/permissions")]
[Authorize]
public sealed class PermissionsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionKeys.Roles.View)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(await sender.Send(new GetPermissionsQuery(), ct));
}
