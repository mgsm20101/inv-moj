using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WIMS.Application.Features.Suppliers;
using WIMS.Domain.Authorization;
using WIMS.WebApi.Common;

namespace WIMS.WebApi.Controllers;

[ApiController]
[Route("api/suppliers")]
[Authorize]
public sealed class SuppliersController(ISender sender) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionKeys.Suppliers.View)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(await sender.Send(new GetSuppliersQuery(), ct));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = PermissionKeys.Suppliers.View)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => (await sender.Send(new GetSupplierByIdQuery(id), ct)).ToActionResult();

    [HttpPost]
    [Authorize(Policy = PermissionKeys.Suppliers.Manage)]
    public async Task<IActionResult> Create([FromBody] CreateSupplierCommand command, CancellationToken ct)
        => (await sender.Send(command, ct)).ToActionResult();

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PermissionKeys.Suppliers.Manage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSupplierCommand body, CancellationToken ct)
        => (await sender.Send(body with { Id = id }, ct)).ToActionResult();
}
