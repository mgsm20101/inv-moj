using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WIMS.Application.Features.StockCounts;
using WIMS.Domain.Authorization;
using WIMS.Domain.Enums;
using WIMS.WebApi.Common;

namespace WIMS.WebApi.Controllers;

[ApiController]
[Route("api/stock-counts")]
[Authorize]
public sealed class StockCountController(ISender sender) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionKeys.StockCount.View)]
    public async Task<IActionResult> GetAll([FromQuery] StockCountStatus? status, CancellationToken ct)
        => Ok(await sender.Send(new GetStockCountsQuery(status), ct));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = PermissionKeys.StockCount.View)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => (await sender.Send(new GetStockCountByIdQuery(id), ct)).ToActionResult();

    [HttpPost]
    [Authorize(Policy = PermissionKeys.StockCount.Manage)]
    public async Task<IActionResult> Plan([FromBody] PlanStockCountCommand command, CancellationToken ct)
        => (await sender.Send(command, ct)).ToActionResult();

    [HttpPost("{id:guid}/freeze")]
    [Authorize(Policy = PermissionKeys.StockCount.Manage)]
    public async Task<IActionResult> Freeze(Guid id, CancellationToken ct)
        => (await sender.Send(new FreezeStockCountCommand(id), ct)).ToActionResult();

    [HttpPost("{id:guid}/count")]
    [Authorize(Policy = PermissionKeys.StockCount.Manage)]
    public async Task<IActionResult> EnterCount(Guid id, [FromBody] IReadOnlyList<CountEntry> entries, CancellationToken ct)
        => (await sender.Send(new EnterCountCommand(id, entries), ct)).ToActionResult();

    [HttpPost("{id:guid}/submit")]
    [Authorize(Policy = PermissionKeys.StockCount.Manage)]
    public async Task<IActionResult> Submit(Guid id, CancellationToken ct)
        => (await sender.Send(new SubmitStockCountCommand(id), ct)).ToActionResult();

    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = PermissionKeys.StockCount.Approve)]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
        => (await sender.Send(new ApproveStockCountCommand(id), ct)).ToActionResult();

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = PermissionKeys.StockCount.Manage)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
        => (await sender.Send(new CancelStockCountCommand(id), ct)).ToActionResult();
}
