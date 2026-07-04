using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WIMS.Application.Features.Custody;
using WIMS.Domain.Authorization;
using WIMS.WebApi.Common;

namespace WIMS.WebApi.Controllers;

/// <summary>العُهد: كشف عهدة وبراءة ذمة.</summary>
[ApiController]
[Route("api/custody")]
[Authorize]
public sealed class CustodyController(ISender sender) : ControllerBase
{
    [HttpGet("statement/{employeeId:guid}")]
    [Authorize(Policy = PermissionKeys.Custody.View)]
    public async Task<IActionResult> Statement(Guid employeeId, [FromQuery] bool includeHistory, CancellationToken ct)
        => (await sender.Send(new GetCustodyStatementQuery(employeeId, includeHistory), ct)).ToActionResult();

    [HttpPost("clear/{employeeId:guid}")]
    [Authorize(Policy = PermissionKeys.Custody.Clear)]
    public async Task<IActionResult> Clear(Guid employeeId, CancellationToken ct)
        => (await sender.Send(new ClearEmployeeCustodyCommand(employeeId), ct)).ToActionResult();

    [HttpPost("items/{custodyItemId:guid}/return")]
    [Authorize(Policy = PermissionKeys.Custody.Manage)]
    public async Task<IActionResult> ReturnItem(Guid custodyItemId, CancellationToken ct)
        => (await sender.Send(new ReturnCustodyItemCommand(custodyItemId), ct)).ToActionResult();
}
