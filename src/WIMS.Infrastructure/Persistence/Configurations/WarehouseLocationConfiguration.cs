using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WIMS.Domain.Warehousing;

namespace WIMS.Infrastructure.Persistence.Configurations;

public sealed class WarehouseLocationConfiguration : IEntityTypeConfiguration<WarehouseLocation>
{
    public void Configure(EntityTypeBuilder<WarehouseLocation> builder)
    {
        builder.ToTable("WarehouseLocations");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Code).HasMaxLength(20).IsRequired();
        builder.Property(l => l.Zone).HasMaxLength(20);
        builder.Property(l => l.Rack).HasMaxLength(20);
        builder.Property(l => l.Bin).HasMaxLength(20);

        builder.Property(l => l.LocationType).HasConversion<byte>();

        // كود الموقع فريد داخل المخزن الواحد.
        builder.HasIndex(l => new { l.WarehouseId, l.Code }).IsUnique();

        builder.HasOne(l => l.Warehouse)
            .WithMany(w => w.Locations)
            .HasForeignKey(l => l.WarehouseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(l => !l.IsDeleted);
    }
}
