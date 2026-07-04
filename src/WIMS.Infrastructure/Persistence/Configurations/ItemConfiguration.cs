using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WIMS.Domain.Catalog;

namespace WIMS.Infrastructure.Persistence.Configurations;

public sealed class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.ToTable("Items");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.ItemCode).HasMaxLength(20).IsRequired();
        builder.Property(i => i.Barcode).HasMaxLength(50);
        builder.Property(i => i.NameAr).HasMaxLength(200).IsRequired();
        builder.Property(i => i.NameEn).HasMaxLength(200);
        builder.Property(i => i.Description).HasMaxLength(500);
        builder.Property(i => i.HazardClass).HasMaxLength(50);
        builder.Property(i => i.StorageConditions).HasMaxLength(200);

        builder.Property(i => i.MinStock).HasPrecision(18, 3);
        builder.Property(i => i.MaxStock).HasPrecision(18, 3);
        builder.Property(i => i.ReorderPoint).HasPrecision(18, 3);
        builder.Property(i => i.ReorderQty).HasPrecision(18, 3);
        builder.Property(i => i.WeightedAvgCost).HasPrecision(18, 4);
        builder.Property(i => i.LastPurchaseCost).HasPrecision(18, 4);

        builder.Property(i => i.ItemType).HasConversion<byte>();

        // BR-01: تفرّد الكود (بعد استبعاد المحذوف منطقياً عبر Query Filter).
        builder.HasIndex(i => i.ItemCode).IsUnique();
        // باركود فريد عند وجوده فقط (Filtered Unique Index).
        builder.HasIndex(i => i.Barcode).IsUnique().HasFilter("[Barcode] IS NOT NULL");

        builder.HasOne(i => i.Category)
            .WithMany(c => c.Items)
            .HasForeignKey(i => i.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.BaseUnit)
            .WithMany(u => u.Items)
            .HasForeignKey(i => i.BaseUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(i => !i.IsDeleted);
    }
}
