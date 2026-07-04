using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WIMS.Application.Features.Transactions.CreateVoucher;
using WIMS.Application.Features.Transactions.Queries;
using WIMS.Application.Features.Transactions.Workflow;
using WIMS.Domain.Authorization;
using WIMS.WebApi.Common;

namespace WIMS.WebApi.Controllers;

/// <summary>حركات المخزون: إنشاء ودورة اعتماد وترحيل السندات.</summary>
[ApiController]
[Route("api/vouchers")]
[Authorize]
public sealed class VouchersController(ISender sender) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionKeys.Vouchers.View)]
    public async Task<IActionResult> GetAll([FromQuery] GetVouchersQuery query, CancellationToken ct)
        => Ok(await sender.Send(query, ct));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = PermissionKeys.Vouchers.View)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => (await sender.Send(new GetVoucherByIdQuery(id), ct)).ToActionResult();

    [HttpPost]
    [Authorize(Policy = PermissionKeys.Vouchers.Create)]
    public async Task<IActionResult> Create([FromBody] CreateVoucherCommand command, CancellationToken ct)
        => (await sender.Send(command, ct)).ToActionResult();

    [HttpPost("{id:guid}/submit")]
    [Authorize(Policy = PermissionKeys.Vouchers.Submit)]
    public async Task<IActionResult> Submit(Guid id, CancellationToken ct)
        => (await sender.Send(new SubmitVoucherCommand(id), ct)).ToActionResult();

    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = PermissionKeys.Vouchers.Approve)]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
        => (await sender.Send(new ApproveVoucherCommand(id), ct)).ToActionResult();

    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = PermissionKeys.Vouchers.Approve)]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectBody body, CancellationToken ct)
        => (await sender.Send(new RejectVoucherCommand(id, body.Reason), ct)).ToActionResult();

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = PermissionKeys.Vouchers.Cancel)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
        => (await sender.Send(new CancelVoucherCommand(id), ct)).ToActionResult();

    [HttpPost("{id:guid}/confirm-transfer")]
    [Authorize(Policy = PermissionKeys.Vouchers.Approve)]
    public async Task<IActionResult> ConfirmTransfer(Guid id, CancellationToken ct)
        => (await sender.Send(new ConfirmTransferReceiptCommand(id), ct)).ToActionResult();

    public sealed record RejectBody(string Reason);
}
