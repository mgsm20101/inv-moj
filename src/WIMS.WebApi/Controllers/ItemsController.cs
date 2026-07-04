using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WIMS.Application.Features.Items.CreateItem;
using WIMS.Application.Features.Items.DeactivateItem;
using WIMS.Application.Features.Items.GetItemById;
using WIMS.Application.Features.Items.GetItems;
using WIMS.Application.Features.Items.UpdateItem;
using WIMS.Domain.Authorization;
using WIMS.WebApi.Common;

namespace WIMS.WebApi.Controllers;

[ApiController]
[Route("api/items")]
[Authorize]
public sealed class ItemsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionKeys.Items.View)]
    public async Task<IActionResult> GetAll([FromQuery] GetItemsQuery query, CancellationToken ct)
        => Ok(await sender.Send(query, ct));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = PermissionKeys.Items.View)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => (await sender.Send(new GetItemByIdQuery(id), ct)).ToActionResult();

    [HttpPost]
    [Authorize(Policy = PermissionKeys.Items.Manage)]
    public async Task<IActionResult> Create([FromBody] CreateItemCommand command, CancellationToken ct)
        => (await sender.Send(command, ct)).ToActionResult();

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PermissionKeys.Items.Manage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateItemCommand command, CancellationToken ct)
        => (await sender.Send(command with { Id = id }, ct)).ToActionResult();

    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Policy = PermissionKeys.Items.Manage)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
        => (await sender.Send(new DeactivateItemCommand(id), ct)).ToActionResult();
}
