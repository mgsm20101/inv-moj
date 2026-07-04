using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WIMS.Application.Features.Alerts;
using WIMS.Domain.Authorization;
using WIMS.Domain.Enums;
using WIMS.WebApi.Common;

namespace WIMS.WebApi.Controllers;

[ApiController]
[Route("api/alerts")]
[Authorize]
public sealed class AlertsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionKeys.Alerts.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] AlertStatus? status, [FromQuery] AlertType? type, CancellationToken ct)
        => Ok(await sender.Send(new GetAlertsQuery(status, type), ct));

    [HttpPost("{id:guid}/acknowledge")]
    [Authorize(Policy = PermissionKeys.Alerts.View)]
    public async Task<IActionResult> Acknowledge(Guid id, CancellationToken ct)
        => (await sender.Send(new AcknowledgeAlertCommand(id), ct)).ToActionResult();

    [HttpPost("{id:guid}/resolve")]
    [Authorize(Policy = PermissionKeys.Alerts.Manage)]
    public async Task<IActionResult> Resolve(Guid id, CancellationToken ct)
        => (await sender.Send(new ResolveAlertCommand(id), ct)).ToActionResult();

    [HttpPost("scan")]
    [Authorize(Policy = PermissionKeys.Alerts.Manage)]
    public async Task<IActionResult> Scan(CancellationToken ct)
        => (await sender.Send(new RunAlertScanCommand(), ct)).ToActionResult();
}
