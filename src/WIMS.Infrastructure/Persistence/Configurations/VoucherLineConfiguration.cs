using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WIMS.Domain.Transactions;

namespace WIMS.Infrastructure.Persistence.Configurations;

public sealed class VoucherLineConfiguration : IEntityTypeConfiguration<VoucherLine>
{
    public void Configure(EntityTypeBuilder<VoucherLine> builder)
    {
        builder.ToTable("VoucherLines");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Qty).HasPrecision(18, 4);
        builder.Property(l => l.QtyAccepted).HasPrecision(18, 4);
        builder.Property(l => l.QtyRejected).HasPrecision(18, 4);
        builder.Property(l => l.UnitCost).HasPrecision(18, 4);
        builder.Property(l => l.BatchNo).HasMaxLength(50);
        builder.Property(l => l.SerialNo).HasMaxLength(100);
        builder.Property(l => l.RejectReason).HasMaxLength(500);
        builder.Property(l => l.Notes).HasMaxLength(500);

        builder.HasIndex(l => l.VoucherId);

        builder.HasOne(l => l.Item).WithMany().HasForeignKey(l => l.ItemId).OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(l => !l.IsDeleted);
    }
}
