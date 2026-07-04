using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WIMS.Domain.Catalog;

namespace WIMS.Infrastructure.Persistence.Configurations;

public sealed class ItemCategoryConfiguration : IEntityTypeConfiguration<ItemCategory>
{
    public void Configure(EntityTypeBuilder<ItemCategory> builder)
    {
        builder.ToTable("ItemCategories");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Code).HasMaxLength(10).IsRequired();
        builder.Property(c => c.NameAr).HasMaxLength(150).IsRequired();
        builder.Property(c => c.NameEn).HasMaxLength(150);
        builder.Property(c => c.Path).HasMaxLength(300).IsRequired();

        builder.HasIndex(c => c.Code).IsUnique();
        builder.HasIndex(c => c.Path);

        builder.HasOne(c => c.Parent)
            .WithMany(c => c.Children)
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}
