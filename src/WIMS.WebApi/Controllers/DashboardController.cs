using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WIMS.Application.Features.Dashboard;
using WIMS.Domain.Authorization;

namespace WIMS.WebApi.Controllers;

/// <summary>لوحة المؤشرات الرئيسية — الأرقام الأساسية والإنذارات (المرحلة 5).</summary>
[ApiController]
[Route("api/dashboard")]
[Authorize(Policy = PermissionKeys.Dashboard.View)]
public sealed class DashboardController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int nearExpiryDays = 30, CancellationToken ct = default)
        => Ok(await sender.Send(new GetDashboardQuery(nearExpiryDays), ct));
}
