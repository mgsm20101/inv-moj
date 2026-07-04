using MediatR;
using Microsoft.EntityFrameworkCore;
using WIMS.Application.Common.Interfaces;
using WIMS.Application.Common.Messaging;
using WIMS.Domain.Enums;
using WIMS.Shared.Results;

namespace WIMS.Application.Features.Alerts;

public sealed record AlertDto(
    Guid Id, AlertType AlertType, AlertSeverity Severity, AlertStatus Status,
    Guid ItemId, string ItemCode, string ItemName, Guid? WarehouseId, string? BatchNo,
    string Message, decimal? ObservedValue, decimal? ThresholdValue, DateTime DetectedAt,
    string? AcknowledgedBy, DateTime? AcknowledgedAt, DateTime? ResolvedAt);

// ─────────────────────── قائمة الإنذارات ───────────────────────
public sealed record GetAlertsQuery(AlertStatus? Status = null, AlertType? Type = null)
    : IQuery<IReadOnlyList<AlertDto>>;

public sealed class GetAlertsHandler(IAppDbContext db) : IRequestHandler<GetAlertsQuery, IReadOnlyList<AlertDto>>
{
    public async Task<IReadOnlyList<AlertDto>> Handle(GetAlertsQuery request, CancellationToken ct)
    {
        var q = db.Alerts.AsNoTracking().AsQueryable();
        if (request.Status is not null) q = q.Where(a => a.Status == request.Status);
        if (request.Type is not null) q = q.Where(a => a.AlertType == request.Type);

        return await q
            .OrderByDescending(a => a.Severity).ThenByDescending(a => a.DetectedAt)
            .Select(a => new AlertDto(
                a.Id, a.AlertType, a.Severity, a.Status, a.ItemId, a.Item.ItemCode, a.Item.NameAr,
                a.WarehouseId, a.BatchNo, a.Message, a.ObservedValue, a.ThresholdValue, a.DetectedAt,
                a.AcknowledgedBy, a.AcknowledgedAt, a.ResolvedAt))
            .ToListAsync(ct);
    }
}

// ─────────────────────── الاطّلاع على إنذار ───────────────────────
public sealed record AcknowledgeAlertCommand(Guid Id) : ICommand<Result>;

public sealed class AcknowledgeAlertHandler(IAppDbContext db, ICurrentUser user)
    : IRequestHandler<AcknowledgeAlertCommand, Result>
{
    public async Task<Result> Handle(AcknowledgeAlertCommand request, CancellationToken ct)
    {
        var a = await db.Alerts.FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (a is null) return Result.Failure(Error.NotFound("Alert", "الإنذار غير موجود."));
        if (a.Status != AlertStatus.Open)
            return Result.Failure(Error.Validation("Alert.Status", "لا يُطّلع إلا على إنذار مفتوح."));

        a.Status = AlertStatus.Acknowledged;
        a.AcknowledgedBy = user.UserName;
        a.AcknowledgedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ─────────────────────── إغلاق إنذار يدوياً ───────────────────────
public sealed record ResolveAlertCommand(Guid Id) : ICommand<Result>;

public sealed class ResolveAlertHandler(IAppDbContext db) : IRequestHandler<ResolveAlertCommand, Result>
{
    public async Task<Result> Handle(ResolveAlertCommand request, CancellationToken ct)
    {
        var a = await db.Alerts.FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (a is null) return Result.Failure(Error.NotFound("Alert", "الإنذار غير موجود."));
        if (a.Status == AlertStatus.Resolved)
            return Result.Failure(Error.Validation("Alert.Status", "الإنذار مغلق مسبقاً."));

        a.Status = AlertStatus.Resolved;
        a.ResolvedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ─────────────────────── فحص يدوي فوري (يُطلق المحرّك عند الطلب) ───────────────────────
public sealed record RunAlertScanCommand : ICommand<Result<AlertScanSummary>>;
public sealed record AlertScanSummary(int Created, int Resolved, int CriticalNew);

public sealed class RunAlertScanHandler(IAlertScanner scanner, IEmailSender email, IAppDbContext db)
    : IRequestHandler<RunAlertScanCommand, Result<AlertScanSummary>>
{
    public async Task<Result<AlertScanSummary>> Handle(RunAlertScanCommand request, CancellationToken ct)
    {
        var result = await scanner.ScanAsync(ct);

        if (result.CriticalNew.Count > 0)
        {
            var body = "إنذارات حرِجة جديدة:\n\n" + string.Join("\n", result.CriticalNew.Select(a => "• " + a.Message));
            await email.SendAsync($"WIMS — {result.CriticalNew.Count} إنذار حرِج جديد", body, ct);
            foreach (var a in result.CriticalNew) a.EmailSent = true;
            await db.SaveChangesAsync(ct);
        }

        return new AlertScanSummary(result.Created, result.Resolved, result.CriticalNew.Count);
    }
}
