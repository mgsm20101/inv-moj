using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WIMS.Domain.Approvals;

namespace WIMS.Infrastructure.Persistence.Configurations;

public sealed class ApprovalWorkflowConfiguration : IEntityTypeConfiguration<ApprovalWorkflow>
{
    public void Configure(EntityTypeBuilder<ApprovalWorkflow> builder)
    {
        builder.ToTable("ApprovalWorkflows");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Name).HasMaxLength(150).IsRequired();
        builder.Property(w => w.TargetType).HasConversion<byte>();
        builder.Property(w => w.VoucherType).HasConversion<byte>();
        builder.Property(w => w.MinAmount).HasPrecision(18, 4);
        builder.Property(w => w.MaxAmount).HasPrecision(18, 4);
        builder.HasMany(w => w.Steps).WithOne(s => s.Workflow).HasForeignKey(s => s.WorkflowId).OnDelete(DeleteBehavior.Cascade);
        builder.HasQueryFilter(w => !w.IsDeleted);
    }
}

public sealed class ApprovalStepConfiguration : IEntityTypeConfiguration<ApprovalStep>
{
    public void Configure(EntityTypeBuilder<ApprovalStep> builder)
    {
        builder.ToTable("ApprovalSteps");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).HasMaxLength(150).IsRequired();
        builder.Property(s => s.ApproverRole).HasMaxLength(150);
        builder.HasIndex(s => new { s.WorkflowId, s.StepOrder }).IsUnique();
        builder.HasQueryFilter(s => !s.IsDeleted);
    }
}

public sealed class ApprovalRequestConfiguration : IEntityTypeConfiguration<ApprovalRequest>
{
    public void Configure(EntityTypeBuilder<ApprovalRequest> builder)
    {
        builder.ToTable("ApprovalRequests");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.TargetType).HasConversion<byte>();
        builder.Property(r => r.Status).HasConversion<byte>();
        builder.Property(r => r.Amount).HasPrecision(18, 4);
        builder.Property(r => r.InitiatedBy).HasMaxLength(256);
        builder.HasIndex(r => new { r.TargetType, r.TargetId });
        builder.HasMany(r => r.Actions).WithOne(a => a.Request).HasForeignKey(a => a.RequestId).OnDelete(DeleteBehavior.Cascade);
        builder.HasQueryFilter(r => !r.IsDeleted);
    }
}

public sealed class ApprovalActionConfiguration : IEntityTypeConfiguration<ApprovalAction>
{
    public void Configure(EntityTypeBuilder<ApprovalAction> builder)
    {
        builder.ToTable("ApprovalActions");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.ActionType).HasConversion<byte>();
        builder.Property(a => a.ActedBy).HasMaxLength(256);
        builder.Property(a => a.OnBehalfOf).HasMaxLength(256);
        builder.Property(a => a.Comment).HasMaxLength(1000);
        builder.HasQueryFilter(a => !a.IsDeleted);
    }
}
