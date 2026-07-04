using MediatR;
using Microsoft.EntityFrameworkCore;
using WIMS.Application.Common.Interfaces;
using WIMS.Application.Common.Messaging;
using WIMS.Application.Features.Approvals;
using WIMS.Application.Features.Transactions.Posting;
using WIMS.Domain.Approvals;
using WIMS.Domain.Enums;
using WIMS.Shared.Results;

namespace WIMS.Application.Features.Transactions.Workflow;

// ─────────────────────── دفع للاعتماد (Draft → UnderReview) + توجيه المسار ───────────────────────
public sealed record SubmitVoucherCommand(Guid Id) : ICommand<Result>;

public sealed class SubmitVoucherHandler(IAppDbContext db, ICurrentUser user, IApprovalRoutingService routing)
    : IRequestHandler<SubmitVoucherCommand, Result>
{
    public async Task<Result> Handle(SubmitVoucherCommand request, CancellationToken ct)
    {
        var v = await db.Vouchers.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (v is null) return Result.Failure(Error.NotFound("Voucher", "السند غير موجود."));
        if (v.Status != VoucherStatus.Draft)
            return Result.Failure(Error.Validation("Voucher.Status", "لا يمكن دفع سند ليس في حالة مسودة."));

        // BR-CUS-03: منع صرف عهدة مستديمة دون تحديد المستلِم.
        if (v.VoucherType == VoucherType.Issue && v.RecipientEmployeeId is null)
        {
            var itemIds = v.Lines.Select(l => l.ItemId).ToList();
            var requiresCustody = await db.Items
                .AnyAsync(i => itemIds.Contains(i.Id) && i.ItemType == ItemType.Durable && i.RequiresCustody, ct);
            if (requiresCustody)
                return Result.Failure(Error.Validation("Voucher.Recipient",
                    "يجب تحديد الموظف المستلِم لصرف صنف مستديم يُدار كعهدة."));
        }

        // ── توجيه مسار الموافقة حسب القيمة ──
        // تكلفة سطر الصرف تُحدَّد وقت الترحيل؛ لذا نُقدّر القيمة من WAC الصنف عند غياب التكلفة.
        var lineItemIds = v.Lines.Select(l => l.ItemId).Distinct().ToList();
        var itemCosts = await db.Items.Where(i => lineItemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, i => i.WeightedAvgCost, ct);
        var amount = v.Lines.Sum(l =>
            l.Qty * (l.UnitCost > 0 ? l.UnitCost : itemCosts.GetValueOrDefault(l.ItemId)));
        var workflow = await routing.FindWorkflowAsync(ApprovalTargetType.Voucher, v.VoucherType, amount, ct);
        var totalSteps = workflow?.Steps.Count is > 0 ? workflow!.Steps.Count : 1;

        db.ApprovalRequests.Add(new ApprovalRequest
        {
            WorkflowId = workflow?.Id ?? Guid.Empty,
            TargetType = ApprovalTargetType.Voucher,
            TargetId = v.Id,
            Amount = amount,
            CurrentStepOrder = 1,
            TotalSteps = totalSteps,
            Status = ApprovalRequestStatus.Pending,
            InitiatedBy = user.UserName,
            InitiatedAt = DateTime.UtcNow,
        });

        v.Status = VoucherStatus.UnderReview;
        v.SubmittedBy = user.UserName;
        v.SubmittedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ─────────────────────── رفض (UnderReview → Rejected) ───────────────────────
public sealed record RejectVoucherCommand(Guid Id, string Reason) : ICommand<Result>;

public sealed class RejectVoucherHandler(IAppDbContext db, ICurrentUser user)
    : IRequestHandler<RejectVoucherCommand, Result>
{
    public async Task<Result> Handle(RejectVoucherCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            return Result.Failure(Error.Validation("Reason", "سبب الرفض مطلوب."));

        var v = await db.Vouchers.FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (v is null) return Result.Failure(Error.NotFound("Voucher", "السند غير موجود."));
        if (v.Status != VoucherStatus.UnderReview)
            return Result.Failure(Error.Validation("Voucher.Status", "لا يمكن رفض سند ليس قيد الاعتماد."));

        var req = await db.ApprovalRequests
            .FirstOrDefaultAsync(r => r.TargetId == v.Id && r.Status == ApprovalRequestStatus.Pending, ct);
        if (req is not null)
        {
            req.Status = ApprovalRequestStatus.Rejected;
            db.ApprovalActions.Add(new ApprovalAction
            {
                RequestId = req.Id, StepOrder = req.CurrentStepOrder,
                ActionType = ApprovalActionType.Reject, ActedBy = user.UserName,
                ActedAt = DateTime.UtcNow, Comment = request.Reason.Trim(),
            });
        }

        v.Status = VoucherStatus.Rejected;
        v.RejectedBy = user.UserName;
        v.RejectedAt = DateTime.UtcNow;
        v.RejectReason = request.Reason.Trim();
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ─────────────────────── إلغاء (Draft/UnderReview → Cancelled) ───────────────────────
public sealed record CancelVoucherCommand(Guid Id) : ICommand<Result>;

public sealed class CancelVoucherHandler(IAppDbContext db)
    : IRequestHandler<CancelVoucherCommand, Result>
{
    public async Task<Result> Handle(CancelVoucherCommand request, CancellationToken ct)
    {
        var v = await db.Vouchers.FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (v is null) return Result.Failure(Error.NotFound("Voucher", "السند غير موجود."));
        if (v.Status is not (VoucherStatus.Draft or VoucherStatus.UnderReview))
            return Result.Failure(Error.Validation("Voucher.Status", "لا يُلغى سند معتمد — يُعكَس فقط بحركة عكسية."));

        v.Status = VoucherStatus.Cancelled;
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ─────────────────────── تأكيد استلام تحويل (InTransit → Received) ───────────────────────
public sealed record ConfirmTransferReceiptCommand(Guid Id) : ICommand<Result>;

public sealed class ConfirmTransferReceiptHandler(
    IAppDbContext db, IVoucherPostingService posting, ICurrentUser user)
    : IRequestHandler<ConfirmTransferReceiptCommand, Result>
{
    public async Task<Result> Handle(ConfirmTransferReceiptCommand request, CancellationToken ct)
    {
        var v = await db.Vouchers.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (v is null) return Result.Failure(Error.NotFound("Voucher", "السند غير موجود."));
        if (v.VoucherType != VoucherType.Transfer)
            return Result.Failure(Error.Validation("Voucher", "السند ليس تحويلاً."));

        var result = await posting.ConfirmTransferReceiptAsync(v, user.UserName ?? "system", ct);
        if (result.IsFailure) return result;

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
