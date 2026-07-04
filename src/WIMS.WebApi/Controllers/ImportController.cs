using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WIMS.Application.Features.Import.ImportEmployees;
using WIMS.Application.Features.Import.ImportItems;
using WIMS.Application.Features.Import.ImportOpeningBalances;
using WIMS.Domain.Authorization;
using WIMS.WebApi.Common;

namespace WIMS.WebApi.Controllers;

/// <summary>استيراد البيانات من Excel — معاينة الأخطاء ثم الاعتماد.</summary>
[ApiController]
[Route("api/import")]
[Authorize(Policy = PermissionKeys.Import.Execute)]
public sealed class ImportController(ISender sender) : ControllerBase
{
    /// <summary>يستورد الأصناف. commit=false يعيد تقرير الأخطاء دون حفظ (معاينة).</summary>
    [HttpPost("items")]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> ImportItems(IFormFile file, [FromQuery] bool commit = false, CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "الملف مطلوب." });

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);

        var result = await sender.Send(new ImportItemsCommand(ms.ToArray(), commit), ct);
        return result.ToActionResult();
    }

    /// <summary>يستورد الأرصدة الافتتاحية. commit=false معاينة، true اعتماد ذرّي.</summary>
    [HttpPost("opening-balances")]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> ImportOpeningBalances(IFormFile file, [FromQuery] bool commit = false, CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "الملف مطلوب." });

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);

        var result = await sender.Send(new ImportOpeningBalancesCommand(ms.ToArray(), commit), ct);
        return result.ToActionResult();
    }

    /// <summary>يستورد الموظفين من تبويب «الموظفون».</summary>
    [HttpPost("employees")]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> ImportEmployees(IFormFile file, [FromQuery] bool commit = false, CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "الملف مطلوب." });

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);

        var result = await sender.Send(new ImportEmployeesCommand(ms.ToArray(), commit), ct);
        return result.ToActionResult();
    }
}
