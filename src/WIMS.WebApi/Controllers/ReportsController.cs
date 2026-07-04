using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WIMS.Application.Common.Interfaces;
using WIMS.Application.Features.Reports;
using WIMS.Domain.Authorization;
using WIMS.Shared.Results;

namespace WIMS.WebApi.Controllers;

/// <summary>
/// تقارير المرحلة 5 — كل تقرير يُصدَّر PDF (الافتراضي) أو Excel أو JSON للعرض.
/// أضِف <c>?format=excel</c> أو <c>?format=json</c> لتغيير الصيغة.
/// </summary>
[ApiController]
[Route("api/reports")]
[Authorize(Policy = PermissionKeys.Reports.View)]
public sealed class ReportsController(ISender sender, IReportExporter exporter) : ControllerBase
{
    /// <summary>تقرير رصيد المخزون.</summary>
    [HttpGet("stock-balance")]
    public async Task<IActionResult> StockBalance(
        [FromQuery] Guid? warehouseId, [FromQuery] bool onlyInStock = true,
        [FromQuery] string? format = null, CancellationToken ct = default)
        => Render(await sender.Send(new StockBalanceReportQuery(warehouseId, onlyInStock), ct), format);

    /// <summary>كارت الصنف (دفتر الحركة).</summary>
    [HttpGet("item-card/{itemId:guid}")]
    public async Task<IActionResult> ItemCard(
        Guid itemId, [FromQuery] Guid? warehouseId,
        [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] string? format = null, CancellationToken ct = default)
        => Render(await sender.Send(new ItemCardReportQuery(itemId, warehouseId, from, to), ct), format);

    /// <summary>الأصناف الراكدة (بلا حركة لمدة تتجاوز العتبة).</summary>
    [HttpGet("stagnant")]
    public async Task<IActionResult> Stagnant(
        [FromQuery] int days = 90, [FromQuery] Guid? warehouseId = null,
        [FromQuery] string? format = null, CancellationToken ct = default)
        => Render(await sender.Send(new StagnantReportQuery(days, warehouseId), ct), format);

    /// <summary>الأصناف تحت الحد الأدنى.</summary>
    [HttpGet("below-min")]
    public async Task<IActionResult> BelowMin(
        [FromQuery] Guid? warehouseId, [FromQuery] string? format = null, CancellationToken ct = default)
        => Render(await sender.Send(new BelowMinReportQuery(warehouseId), ct), format);

    /// <summary>العُهد الشخصية النشطة.</summary>
    [HttpGet("custody")]
    public async Task<IActionResult> Custody(
        [FromQuery] Guid? employeeId, [FromQuery] string? format = null, CancellationToken ct = default)
        => Render(await sender.Send(new CustodyReportQuery(employeeId), ct), format);

    /// <summary>محضر جرد.</summary>
    [HttpGet("stock-count/{stockCountId:guid}")]
    public async Task<IActionResult> StockCountMinutes(
        Guid stockCountId, [FromQuery] string? format = null, CancellationToken ct = default)
        => Render(await sender.Send(new StockCountMinutesReportQuery(stockCountId), ct), format);

    /// <summary>يحوّل نتيجة التقرير إلى ملف (PDF/Excel) أو JSON، أو خطأ HTTP.</summary>
    private IActionResult Render(Result<ReportDocument> result, string? format)
    {
        if (!result.IsSuccess)
            return Problem(title: result.Error.Code, detail: result.Error.Message, statusCode: 404);

        var doc = result.Value;
        if (string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
            return Ok(doc);

        var fmt = format is not null && (format.Equals("excel", StringComparison.OrdinalIgnoreCase)
                                      || format.Equals("xlsx", StringComparison.OrdinalIgnoreCase))
            ? ReportFormat.Excel
            : ReportFormat.Pdf;

        var file = exporter.Export(doc, fmt);
        return File(file.Content, file.ContentType, file.FileName);
    }
}
