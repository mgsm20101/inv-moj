using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WIMS.Domain.Custody;

namespace WIMS.Infrastructure.Persistence.Configurations;

public sealed class CustodyConfiguration : IEntityTypeConfiguration<Custody>
{
    public void Configure(EntityTypeBuilder<Custody> builder)
    {
        builder.ToTable("Custodies");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.CustodyNo).HasMaxLength(20).IsRequired();
        builder.Property(c => c.CustodyType).HasConversion<byte>();
        builder.Property(c => c.Status).HasConversion<byte>();
        builder.Property(c => c.Notes).HasMaxLength(500);

        builder.HasIndex(c => c.CustodyNo).IsUnique();
        // ملف نشط واحد لكل موظف.
        builder.HasIndex(c => new { c.EmployeeId, c.Status })
            .IsUnique()
            .HasFilter("[EmployeeId] IS NOT NULL AND [Status] = 1");

        builder.HasOne(c => c.Employee).WithMany().HasForeignKey(c => c.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(c => c.Items).WithOne(i => i.Custody).HasForeignKey(i => i.CustodyId).OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}

public sealed class CustodyItemConfiguration : IEntityTypeConfiguration<CustodyItem>
{
    public void Configure(EntityTypeBuilder<CustodyItem> builder)
    {
        builder.ToTable("CustodyItems");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.SerialNo).HasMaxLength(100);
        builder.Property(i => i.Qty).HasPrecision(18, 4);
        builder.Property(i => i.UnitCost).HasPrecision(18, 4);
        builder.Property(i => i.Status).HasConversion<byte>();
        builder.Property(i => i.ConditionNote).HasMaxLength(300);

        // BR-CUS-02: سيريال واحد = بند نشط واحد.
        builder.HasIndex(i => i.SerialNo)
            .IsUnique()
            .HasFilter("[SerialNo] IS NOT NULL AND [Status] = 1");

        builder.HasIndex(i => i.SourceStockTransactionId);

        builder.HasOne(i => i.Item).WithMany().HasForeignKey(i => i.ItemId).OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(i => !i.IsDeleted);
    }
}
