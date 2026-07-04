using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WIMS.Domain.Transactions;

namespace WIMS.Infrastructure.Persistence.Configurations;

public sealed class StockTransactionConfiguration : IEntityTypeConfiguration<StockTransaction>
{
    public void Configure(EntityTypeBuilder<StockTransaction> builder)
    {
        builder.ToTable("StockTransactions");
        builder.HasKey(t => t.Id);

        // تسلسل عالمي للترتيب الزمني القاطع للدفتر.
        builder.Property(t => t.TransactionNo)
            .HasDefaultValueSql("NEXT VALUE FOR dbo.StockTxnSeq");

        builder.Property(t => t.TxnType).HasConversion<byte>();
        builder.Property(t => t.BatchNo).HasMaxLength(50);
        builder.Property(t => t.SerialNo).HasMaxLength(100);
        builder.Property(t => t.PostedBy).HasMaxLength(256).IsRequired();

        builder.Property(t => t.Qty).HasPrecision(18, 4);
        builder.Property(t => t.UnitCost).HasPrecision(18, 4);
        builder.Property(t => t.TotalCost).HasPrecision(18, 4);
        builder.Property(t => t.QtyOnHandAfter).HasPrecision(18, 4);
        builder.Property(t => t.WacAfter).HasPrecision(18, 4);

        builder.HasIndex(t => t.TransactionNo).IsUnique();
        builder.HasIndex(t => new { t.ItemId, t.WarehouseId });
        builder.HasIndex(t => t.VoucherId);

        builder.HasOne(t => t.Voucher).WithMany().HasForeignKey(t => t.VoucherId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.Item).WithMany().HasForeignKey(t => t.ItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(t => t.Warehouse).WithMany().HasForeignKey(t => t.WarehouseId).OnDelete(DeleteBehavior.Restrict);

        // الدفتر غير قابل للحذف المنطقي (append-only).
        builder.HasQueryFilter(t => !t.IsDeleted);
    }
}
