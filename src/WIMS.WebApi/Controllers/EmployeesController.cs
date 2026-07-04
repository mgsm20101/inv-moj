using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WIMS.Application.Features.Employees;
using WIMS.Domain.Authorization;
using WIMS.Domain.Enums;
using WIMS.WebApi.Common;

namespace WIMS.WebApi.Controllers;

[ApiController]
[Route("api/employees")]
[Authorize]
public sealed class EmployeesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionKeys.Employees.View)]
    public async Task<IActionResult> GetAll([FromQuery] string? search, CancellationToken ct)
        => Ok(await sender.Send(new GetEmployeesQuery(search), ct));

    [HttpPost]
    [Authorize(Policy = PermissionKeys.Employees.Manage)]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeCommand command, CancellationToken ct)
        => (await sender.Send(command, ct)).ToActionResult();

    [HttpPost("{id:guid}/status")]
    [Authorize(Policy = PermissionKeys.Employees.Manage)]
    public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] StatusBody body, CancellationToken ct)
        => (await sender.Send(new ChangeEmployeeStatusCommand(id, body.Status), ct)).ToActionResult();

    public sealed record StatusBody(EmployeeStatus Status);
}
