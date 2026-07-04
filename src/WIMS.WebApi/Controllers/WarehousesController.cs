using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WIMS.Application.Features.Warehouses;
using WIMS.Domain.Authorization;
using WIMS.WebApi.Common;

namespace WIMS.WebApi.Controllers;

[ApiController]
[Route("api/warehouses")]
[Authorize]
public sealed class WarehousesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionKeys.Warehouses.View)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(await sender.Send(new GetWarehousesQuery(), ct));

    [HttpPost]
    [Authorize(Policy = PermissionKeys.Warehouses.Manage)]
    public async Task<IActionResult> Create([FromBody] CreateWarehouseCommand command, CancellationToken ct)
        => (await sender.Send(command, ct)).ToActionResult();
}
