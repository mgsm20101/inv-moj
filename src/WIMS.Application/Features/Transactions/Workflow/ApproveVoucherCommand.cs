using MediatR;
using Microsoft.EntityFrameworkCore;
using WIMS.Application.Common.Interfaces;
using WIMS.Application.Common.Messaging;
using WIMS.Application.Features.Custody;
using WIMS.Application.Features.Transactions.Posting;
using WIMS.Domain.Approvals;
using WIMS.Domain.Enums;
using WIMS.Domain.Transactions;
using WIMS.Shared.Results;

namespace WIMS.Application.Features.Transactions.Workflow;

/// <summary>اعتماد خطوة الموافقة الحالية. عند آخر خطوة: ترحيل ذرّي + توفير عهدة.</summary>
public sealed record ApproveVoucherCommand(Guid Id) : ICommand<Result>;

public sealed class ApproveVoucherHandler(
    IAppDbContext db,
    IVoucherPostingService posting,
    ICustodyProvisioningService custody,
    ICurrentUser user)
    : IRequestHandler<ApproveVoucherCommand, Result>
{
    private static readonly Dictionary<VoucherType, string> Prefixes = new()
    {
        [VoucherType.Receipt] = "GRN",
        [VoucherType.Issue] = "ISS",
        [VoucherType.Transfer] = "TRF",
        [VoucherType.Return] = "RTN",
        [VoucherType.Adjustment] = "ADJ",
        [VoucherType.Reversal] = "REV",
    };

    public async Task<Result> Handle(ApproveVoucherCommand request, CancellationToken ct)
    {
        var v = await db.Vouchers.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (v is null) return Result.Failure(Error.NotFound("Voucher", "السند غير موجود."));
        if (v.Status != VoucherStatus.UnderReview)
            return Result.Failure(Error.Validation("Voucher.Status", "لا يُعتمد سند ليس قيد الاعتماد."));

        var currentUser = user.UserName ?? "system";

        var req = await db.ApprovalRequests
            .FirstOrDefaultAsync(r => r.TargetId == v.Id && r.Status == ApprovalRequestStatus.Pending, ct);

        // بوابة الدور: يجب أن يملك المُعتمِد الدور المطلوب لهذه الخطوة (مدير النظام يتجاوز).
        if (req is not null && req.WorkflowId != Guid.Empty)
        {
            var requiredRole = await db.ApprovalSteps
                .Where(s => s.WorkflowId == req.WorkflowId && s.StepOrder == req.CurrentStepOrder)
                .Select(s => s.ApproverRole)
                .FirstOrDefaultAsync(ct);
            if (!string.IsNullOrWhiteSpace(requiredRole)
                && !user.IsInRole(requiredRole)
                && !user.IsInRole(Domain.Authorization.SystemRoles.Admin))
                return Result.Failure(Error.Forbidden("Approval.Role",
                    $"هذه الخطوة تتطلّب دور '{requiredRole}'."));
        }

        // منع اعتماد نفس المستخدم خطوتين متتاليتين.
        if (req is not null)
        {
            var lastApprover = await db.ApprovalActions
                .Where(a => a.RequestId == req.Id && a.ActionType == ApprovalActionType.Approve)
                .OrderByDescending(a => a.ActedAt).Select(a => a.ActedBy).FirstOrDefaultAsync(ct);
            if (lastApprover is not null && string.Equals(lastApprover, currentUser, StringComparison.OrdinalIgnoreCase))
                return Result.Failure(Error.Forbidden("Approval.SoD", "لا يجوز اعتماد خطوتين متتاليتين لنفس المستخدم."));

            db.ApprovalActions.Add(new ApprovalAction
            {
                RequestId = req.Id, StepOrder = req.CurrentStepOrder,
                ActionType = ApprovalActionType.Approve, ActedBy = currentUser, ActedAt = DateTime.UtcNow,
            });

            // ليست الخطوة الأخيرة → تقدّم فقط.
            if (req.CurrentStepOrder < req.TotalSteps)
            {
                req.CurrentStepOrder++;
                await db.SaveChangesAsync(ct);
                return Result.Success();
            }

            req.Status = ApprovalRequestStatus.Approved;
        }

        // ── الخطوة الأخيرة: ترحيل ذرّي + توفير عهدة ──
        var posted = await posting.PostAsync(v, currentUser, ct);
        if (posted.IsFailure) return posted;

        var provisioned = await custody.ProvisionAsync(v, currentUser, ct);
        if (provisioned.IsFailure) return provisioned;

        v.Status = VoucherStatus.Approved;
        v.ApprovedBy = currentUser;
        v.ApprovedAt = DateTime.UtcNow;
        v.PostedAt = DateTime.UtcNow;
        v.VoucherNo = await GenerateVoucherNoAsync(v.VoucherType, ct);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(Error.Conflict("Stock.Concurrency",
                "تعذّر إتمام الاعتماد بسبب تحديث متزامن على الرصيد؛ أعد المحاولة."));
        }

        return Result.Success();
    }

    private async Task<string> GenerateVoucherNoAsync(VoucherType type, CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = Prefixes[type];
        var count = await db.Vouchers.CountAsync(x => x.VoucherType == type && x.Status == VoucherStatus.Approved, ct);
        return $"{prefix}-{year}-{(count + 1):D6}";
    }
}
