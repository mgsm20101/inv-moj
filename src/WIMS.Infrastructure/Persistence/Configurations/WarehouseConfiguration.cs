using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WIMS.Domain.Warehousing;

namespace WIMS.Infrastructure.Persistence.Configurations;

public sealed class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable("Warehouses");
        builder.HasKey(w => w.Id);

        builder.Property(w => w.Code).HasMaxLength(10).IsRequired();
        builder.Property(w => w.NameAr).HasMaxLength(150).IsRequired();
        builder.Property(w => w.Region).HasMaxLength(100);

        builder.Property(w => w.WarehouseType).HasConversion<byte>();
        builder.Property(w => w.Status).HasConversion<byte>();

        builder.HasIndex(w => w.Code).IsUnique();

        builder.HasQueryFilter(w => !w.IsDeleted);
    }
}
