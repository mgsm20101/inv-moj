using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using WIMS.Application.Common.Interfaces;

namespace WIMS.Infrastructure.Persistence;

/// <summary>
/// مصنع وقت التصميم — يستخدمه أمر `dotnet ef` لإنشاء AppDbContext دون تشغيل تطبيق الويب.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=WIMS;Trusted_Connection=True;TrustServerCertificate=True",
                sql => sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName))
            .Options;

        return new AppDbContext(options, new DesignTimeCurrentUser());
    }

    /// <summary>مستخدم صوري لوقت التصميم فقط.</summary>
    private sealed class DesignTimeCurrentUser : ICurrentUser
    {
        public string? UserId => null;
        public string? UserName => "design-time";
        public bool IsAuthenticated => false;
        public bool HasPermission(string permissionKey) => false;
        public bool IsInRole(string role) => false;
    }
}
