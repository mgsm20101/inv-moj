using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WIMS.Application.Features.Transactions.Queries;
using WIMS.Domain.Authorization;

namespace WIMS.WebApi.Controllers;

/// <summary>الأرصدة والدفتر (Ledger).</summary>
[ApiController]
[Route("api/stock")]
[Authorize]
public sealed class StockController(ISender sender) : ControllerBase
{
    [HttpGet("balances")]
    [Authorize(Policy = PermissionKeys.Items.View)]
    public async Task<IActionResult> Balances([FromQuery] GetStockBalancesQuery query, CancellationToken ct)
        => Ok(await sender.Send(query, ct));

    [HttpGet("ledger")]
    [Authorize(Policy = PermissionKeys.Vouchers.View)]
    public async Task<IActionResult> Ledger([FromQuery] GetLedgerQuery query, CancellationToken ct)
        => Ok(await sender.Send(query, ct));
}
