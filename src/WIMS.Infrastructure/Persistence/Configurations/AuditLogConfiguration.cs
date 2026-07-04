using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WIMS.Domain.Auditing;

namespace WIMS.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.UserId).HasMaxLength(450);
        builder.Property(a => a.UserName).HasMaxLength(256);
        builder.Property(a => a.Entity).HasMaxLength(200).IsRequired();
        builder.Property(a => a.EntityId).HasMaxLength(450);
        builder.Property(a => a.Action).HasMaxLength(50).IsRequired();
        builder.Property(a => a.EditedBy).HasMaxLength(256);
        builder.Property(a => a.EditReason).HasMaxLength(1000);

        builder.HasIndex(a => a.Timestamp);
        builder.HasIndex(a => a.Entity);
    }
}
