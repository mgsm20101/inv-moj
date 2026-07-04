using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WIMS.Application.Features.Categories.CreateCategory;
using WIMS.Application.Features.Categories.GetCategories;
using WIMS.Application.Features.Categories.UpdateCategory;
using WIMS.Application.Features.Units;
using WIMS.Domain.Authorization;
using WIMS.WebApi.Common;

namespace WIMS.WebApi.Controllers;

/// <summary>البيانات المرجعية: التصنيفات ووحدات القياس.</summary>
[ApiController]
[Route("api")]
[Authorize]
public sealed class CatalogController(ISender sender) : ControllerBase
{
    [HttpGet("categories")]
    [Authorize(Policy = PermissionKeys.Items.View)]
    public async Task<IActionResult> GetCategories(CancellationToken ct)
        => Ok(await sender.Send(new GetCategoriesQuery(), ct));

    [HttpPost("categories")]
    [Authorize(Policy = PermissionKeys.Items.Manage)]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryCommand command, CancellationToken ct)
        => (await sender.Send(command, ct)).ToActionResult();

    [HttpPut("categories/{id:guid}")]
    [Authorize(Policy = PermissionKeys.Items.Manage)]
    public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] UpdateCategoryCommand command, CancellationToken ct)
        => (await sender.Send(command with { Id = id }, ct)).ToActionResult();

    [HttpGet("units")]
    [Authorize(Policy = PermissionKeys.Items.View)]
    public async Task<IActionResult> GetUnits(CancellationToken ct)
        => Ok(await sender.Send(new GetUnitsQuery(), ct));

    [HttpPost("units")]
    [Authorize(Policy = PermissionKeys.Items.Manage)]
    public async Task<IActionResult> CreateUnit([FromBody] CreateUnitCommand command, CancellationToken ct)
        => (await sender.Send(command, ct)).ToActionResult();

    [HttpPut("units/{id:guid}")]
    [Authorize(Policy = PermissionKeys.Items.Manage)]
    public async Task<IActionResult> UpdateUnit(Guid id, [FromBody] UpdateUnitCommand command, CancellationToken ct)
        => (await sender.Send(command with { Id = id }, ct)).ToActionResult();
}
