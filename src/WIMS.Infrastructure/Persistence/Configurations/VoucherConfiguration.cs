using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WIMS.Domain.Transactions;

namespace WIMS.Infrastructure.Persistence.Configurations;

public sealed class VoucherConfiguration : IEntityTypeConfiguration<Voucher>
{
    public void Configure(EntityTypeBuilder<Voucher> builder)
    {
        builder.ToTable("Vouchers");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.VoucherNo).HasMaxLength(30).IsRequired();
        builder.Property(v => v.VoucherType).HasConversion<byte>();
        builder.Property(v => v.Status).HasConversion<byte>();
        builder.Property(v => v.AdjustmentType).HasConversion<byte>();
        builder.Property(v => v.TransferStatus).HasConversion<byte>();
        builder.Property(v => v.ReferenceNo).HasMaxLength(100);
        builder.Property(v => v.CostCenter).HasMaxLength(100);
        builder.Property(v => v.RequestingDept).HasMaxLength(150);
        builder.Property(v => v.Reason).HasMaxLength(500);
        builder.Property(v => v.RejectReason).HasMaxLength(500);
        builder.Property(v => v.Notes).HasMaxLength(1000);
        builder.Property(v => v.SubmittedBy).HasMaxLength(256);
        builder.Property(v => v.ApprovedBy).HasMaxLength(256);
        builder.Property(v => v.RejectedBy).HasMaxLength(256);

        builder.HasIndex(v => v.VoucherNo);
        builder.HasIndex(v => new { v.VoucherType, v.Status });

        builder.HasOne(v => v.Warehouse).WithMany().HasForeignKey(v => v.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(v => v.ToWarehouse).WithMany().HasForeignKey(v => v.ToWarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(v => v.Supplier).WithMany().HasForeignKey(v => v.SupplierId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(v => v.Lines).WithOne(l => l.Voucher).HasForeignKey(l => l.VoucherId).OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(v => !v.IsDeleted);
    }
}
