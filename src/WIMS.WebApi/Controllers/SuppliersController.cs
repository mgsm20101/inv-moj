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

    [HttpPost]
    [Authorize(Policy = PermissionKeys.Suppliers.Manage)]
    public async Task<IActionResult> Create([FromBody] CreateSupplierCommand command, CancellationToken ct)
        => (await sender.Send(command, ct)).ToActionResult();
}
