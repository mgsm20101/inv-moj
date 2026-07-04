using WIMS.Domain.Common;
using WIMS.Domain.Enums;

namespace WIMS.Domain.Approvals;

/// <summary>مسار موافقة قابل للتهيئة — يُختار حسب الهدف/النوع/نطاق القيمة.</summary>
public class ApprovalWorkflow : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public ApprovalTargetType TargetType { get; set; }
    public VoucherType? VoucherType { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<ApprovalStep> Steps { get; set; } = new List<ApprovalStep>();
}

/// <summary>خطوة اعتماد ضمن مسار (بترتيب). المعتمِد دور أو مستخدم بعينه.</summary>
public class ApprovalStep : BaseEntity
{
    public Guid WorkflowId { get; set; }
    public ApprovalWorkflow Workflow { get; set; } = null!;

    public int StepOrder { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>دور المعتمِد (اسم الدور) أو مستخدم محدّد.</summary>
    public string? ApproverRole { get; set; }
    public Guid? ApproverUserId { get; set; }
}

/// <summary>طلب موافقة حيّ مربوط بهدف (سند/نقل عهدة/براءة).</summary>
public class ApprovalRequest : BaseEntity
{
    public Guid WorkflowId { get; set; }
    public ApprovalTargetType TargetType { get; set; }
    public Guid TargetId { get; set; }

    public decimal Amount { get; set; }
    public int CurrentStepOrder { get; set; } = 1;
    public int TotalSteps { get; set; }

    public ApprovalRequestStatus Status { get; set; } = ApprovalRequestStatus.Pending;
    public string? InitiatedBy { get; set; }
    public DateTime InitiatedAt { get; set; }

    public ICollection<ApprovalAction> Actions { get; set; } = new List<ApprovalAction>();
}

/// <summary>سجل إجراء موافقة — أثر تدقيقي كامل لكل خطوة.</summary>
public class ApprovalAction : BaseEntity
{
    public Guid RequestId { get; set; }
    public ApprovalRequest Request { get; set; } = null!;

    public int StepOrder { get; set; }
    public ApprovalActionType ActionType { get; set; }
    public string? ActedBy { get; set; }
    public DateTime ActedAt { get; set; }
    public string? OnBehalfOf { get; set; }
    public string? Comment { get; set; }
}
