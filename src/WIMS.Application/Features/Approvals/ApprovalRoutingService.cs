using Microsoft.EntityFrameworkCore;
using WIMS.Application.Common.Interfaces;
using WIMS.Domain.Approvals;
using WIMS.Domain.Enums;

namespace WIMS.Application.Features.Approvals;

public sealed class ApprovalRoutingService(IAppDbContext db) : IApprovalRoutingService
{
    public async Task<ApprovalWorkflow?> FindWorkflowAsync(
        ApprovalTargetType targetType, VoucherType? voucherType, decimal amount, CancellationToken ct)
    {
        var candidates = await db.ApprovalWorkflows
            .Include(w => w.Steps)
            .Where(w => w.IsActive && w.TargetType == targetType)
            .ToListAsync(ct);

        return candidates
            .Where(w => w.VoucherType == null || w.VoucherType == voucherType)
            .Where(w => (w.MinAmount == null || amount >= w.MinAmount) && (w.MaxAmount == null || amount < w.MaxAmount))
            // الأكثر تخصيصاً أولاً: مطابقة النوع ثم أضيق نطاق.
            .OrderByDescending(w => w.VoucherType != null)
            .ThenByDescending(w => w.MinAmount ?? 0)
            .FirstOrDefault();
    }
}
