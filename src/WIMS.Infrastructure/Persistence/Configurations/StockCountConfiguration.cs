using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WIMS.Domain.Inventory;

namespace WIMS.Infrastructure.Persistence.Configurations;

public sealed class StockCountConfiguration : IEntityTypeConfiguration<StockCount>
{
    public void Configure(EntityTypeBuilder<StockCount> builder)
    {
        builder.ToTable("StockCounts");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.CountNo).HasMaxLength(30).IsRequired();
        builder.Property(c => c.CountType).HasConversion<byte>();
        builder.Property(c => c.Status).HasConversion<byte>();
        builder.Property(c => c.ScopeNote).HasMaxLength(500);
        builder.Property(c => c.FrozenBy).HasMaxLength(256);
        builder.Property(c => c.ApprovedBy).HasMaxLength(256);
        builder.Property(c => c.AdjustmentVoucherNos).HasMaxLength(300);
        builder.Property(c => c.Notes).HasMaxLength(1000);

        builder.HasIndex(c => c.CountNo).IsUnique();
        builder.HasIndex(c => new { c.WarehouseId, c.Status });

        builder.HasOne(c => c.Warehouse).WithMany()
            .HasForeignKey(c => c.WarehouseId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Lines).WithOne(l => l.StockCount)
            .HasForeignKey(l => l.StockCountId).OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}

public sealed class StockCountLineConfiguration : IEntityTypeConfiguration<StockCountLine>
{
    public void Configure(EntityTypeBuilder<StockCountLine> builder)
    {
        builder.ToTable("StockCountLines");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.BatchNo).HasMaxLength(50);
        builder.Property(l => l.SerialNo).HasMaxLength(100);
        builder.Property(l => l.CountedBy).HasMaxLength(256);
        builder.Property(l => l.Notes).HasMaxLength(500);

        builder.Property(l => l.BookQty).HasPrecision(18, 3);
        builder.Property(l => l.PhysicalQty).HasPrecision(18, 3);
        builder.Property(l => l.VarianceQty).HasPrecision(18, 3);
        builder.Property(l => l.UnitCost).HasPrecision(18, 4);
        builder.Property(l => l.VarianceValue).HasPrecision(18, 4);

        builder.HasIndex(l => l.StockCountId);

        builder.HasOne(l => l.Item).WithMany()
            .HasForeignKey(l => l.ItemId).OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(l => !l.IsDeleted);
    }
}
