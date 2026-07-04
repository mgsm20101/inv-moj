using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WIMS.Domain.Inventory;

namespace WIMS.Infrastructure.Persistence.Configurations;

public sealed class StockBalanceConfiguration : IEntityTypeConfiguration<StockBalance>
{
    public void Configure(EntityTypeBuilder<StockBalance> builder)
    {
        builder.ToTable("StockBalances");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.BatchNo).HasMaxLength(50);
        builder.Property(s => s.SerialNo).HasMaxLength(100);

        builder.Property(s => s.QtyOnHand).HasPrecision(18, 3);
        builder.Property(s => s.QtyReserved).HasPrecision(18, 3);
        builder.Property(s => s.AvgCost).HasPrecision(18, 4);

        // عمود محسوب ومخزَّن في قاعدة البيانات.
        builder.Property(s => s.QtyAvailable)
            .HasColumnType("decimal(18,3)")
            .HasComputedColumnSql("[QtyOnHand] - [QtyReserved]", stored: true);

        // Concurrency Token (BR-11).
        builder.Property(s => s.RowVersion).IsRowVersion();

        // سطر رصيد واحد لكل تركيبة (صنف × مخزن × موقع × دُفعة × سيريال).
        builder.HasIndex(s => new { s.ItemId, s.WarehouseId, s.LocationId, s.BatchNo, s.SerialNo })
            .IsUnique();

        builder.HasOne(s => s.Item)
            .WithMany(i => i.StockBalances)
            .HasForeignKey(s => s.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Warehouse)
            .WithMany()
            .HasForeignKey(s => s.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Location)
            .WithMany()
            .HasForeignKey(s => s.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(s => !s.IsDeleted);
    }
}
