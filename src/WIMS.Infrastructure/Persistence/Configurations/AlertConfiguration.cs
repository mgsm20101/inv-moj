using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WIMS.Domain.Alerts;

namespace WIMS.Infrastructure.Persistence.Configurations;

public sealed class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
    public void Configure(EntityTypeBuilder<Alert> builder)
    {
        builder.ToTable("Alerts");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.AlertType).HasConversion<byte>();
        builder.Property(a => a.Severity).HasConversion<byte>();
        builder.Property(a => a.Status).HasConversion<byte>();
        builder.Property(a => a.BatchNo).HasMaxLength(50);
        builder.Property(a => a.Message).HasMaxLength(500).IsRequired();
        builder.Property(a => a.DedupKey).HasMaxLength(200).IsRequired();
        builder.Property(a => a.AcknowledgedBy).HasMaxLength(256);

        builder.Property(a => a.ObservedValue).HasPrecision(18, 3);
        builder.Property(a => a.ThresholdValue).HasPrecision(18, 3);

        // إزالة التكرار: لا يوجد إنذاران مفتوحان بنفس المفتاح (يُفحَص في الكود).
        builder.HasIndex(a => new { a.Status, a.DedupKey });
        builder.HasIndex(a => new { a.Status, a.AlertType });

        builder.HasOne(a => a.Item).WithMany()
            .HasForeignKey(a => a.ItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.Warehouse).WithMany()
            .HasForeignKey(a => a.WarehouseId).OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(a => !a.IsDeleted);
    }
}
