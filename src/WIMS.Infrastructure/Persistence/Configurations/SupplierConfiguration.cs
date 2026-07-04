using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WIMS.Domain.Suppliers;

namespace WIMS.Infrastructure.Persistence.Configurations;

public sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("Suppliers");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Code).HasMaxLength(20).IsRequired();
        builder.Property(s => s.NameAr).HasMaxLength(200).IsRequired();
        builder.Property(s => s.NameEn).HasMaxLength(200);
        builder.Property(s => s.TaxNumber).HasMaxLength(50);
        builder.Property(s => s.CommercialReg).HasMaxLength(50);
        builder.Property(s => s.ContactPerson).HasMaxLength(150);
        builder.Property(s => s.Phone).HasMaxLength(30);
        builder.Property(s => s.Email).HasMaxLength(150);
        builder.Property(s => s.Address).HasMaxLength(300);

        builder.HasIndex(s => s.Code).IsUnique();
        builder.HasQueryFilter(s => !s.IsDeleted);
    }
}
