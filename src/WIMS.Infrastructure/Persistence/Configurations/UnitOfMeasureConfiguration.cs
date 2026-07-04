using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WIMS.Domain.Catalog;

namespace WIMS.Infrastructure.Persistence.Configurations;

public sealed class UnitOfMeasureConfiguration : IEntityTypeConfiguration<UnitOfMeasure>
{
    public void Configure(EntityTypeBuilder<UnitOfMeasure> builder)
    {
        builder.ToTable("UnitsOfMeasure");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Code).HasMaxLength(10).IsRequired();
        builder.Property(u => u.NameAr).HasMaxLength(50).IsRequired();

        builder.HasIndex(u => u.Code).IsUnique();

        builder.HasQueryFilter(u => !u.IsDeleted);
    }
}
