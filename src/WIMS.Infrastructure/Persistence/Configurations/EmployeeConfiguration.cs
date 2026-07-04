using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WIMS.Domain.Employees;

namespace WIMS.Infrastructure.Persistence.Configurations;

public sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.EmployeeNo).HasMaxLength(20).IsRequired();
        builder.Property(e => e.NationalId).HasMaxLength(10).IsRequired();
        builder.Property(e => e.FullNameAr).HasMaxLength(150).IsRequired();
        builder.Property(e => e.FullNameEn).HasMaxLength(150);
        builder.Property(e => e.Department).HasMaxLength(120).IsRequired();
        builder.Property(e => e.JobTitle).HasMaxLength(120);
        builder.Property(e => e.CostCenter).HasMaxLength(30).IsRequired();
        builder.Property(e => e.Email).HasMaxLength(150);
        builder.Property(e => e.Phone).HasMaxLength(20);
        builder.Property(e => e.Status).HasConversion<byte>();

        builder.HasIndex(e => e.EmployeeNo).IsUnique();
        builder.HasIndex(e => e.NationalId).IsUnique();
        builder.HasIndex(e => e.UserId).IsUnique().HasFilter("[UserId] IS NOT NULL");

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
