using WIMS.Domain.Approvals;
using WIMS.Domain.Enums;

namespace WIMS.Application.Features.Approvals;

/// <summary>يختار مسار الموافقة المطابق حسب الهدف/النوع/نطاق القيمة.</summary>
public interface IApprovalRoutingService
{
    Task<ApprovalWorkflow?> FindWorkflowAsync(
        ApprovalTargetType targetType, VoucherType? voucherType, decimal amount, CancellationToken ct = default);
}
