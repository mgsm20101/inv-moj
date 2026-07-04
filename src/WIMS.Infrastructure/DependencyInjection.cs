using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WIMS.Application.Common.Interfaces;
using WIMS.Application.Features.Alerts;
using WIMS.Infrastructure.Identity;
using WIMS.Infrastructure.Persistence;
using WIMS.Infrastructure.Services;

namespace WIMS.Infrastructure;

/// <summary>تسجيل خدمات طبقة Infrastructure: EF Core + Identity + JWT + الخدمات.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("سلسلة الاتصال 'Default' غير موجودة في الإعدادات.");

        services.AddDbContext<AppDbContext>(options =>
            options
                .UseSqlServer(connectionString, sql => sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName))
                // الحذف الناعم على أطراف مطلوبة في العلاقات أمر مقصود عبر النظام.
                .ConfigureWarnings(w => w.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning)));

        // إتاحة السياق لطبقة Application عبر التجريد.
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        // ASP.NET Core Identity (Core فقط — بلا Cookies لأن المصادقة عبر JWT).
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                // سياسة كلمة المرور.
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;

                // القفل بعد المحاولات الفاشلة.
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.AllowedForNewUsers = true;

                options.User.RequireUniqueEmail = false;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        // JWT.
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IIdentityAdminService, IdentityAdminService>();

        // ── المرحلة 1: خدمات الكتالوج والاستيراد ──
        services.AddScoped<IItemCodeGenerator, Services.ItemCodeGenerator>();
        services.AddSingleton<IExcelReader, Services.ExcelReader>();

        // ── المرحلة 4: الإنذارات (بريد SMTP + عتبات الفحص) ──
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        var alertOptions = configuration.GetSection("Alerts").Get<AlertScanOptions>() ?? new AlertScanOptions();
        services.AddSingleton(alertOptions);

        // ── المرحلة 5: تصدير التقارير (PDF QuestPDF RTL + Excel ClosedXML) ──
        services.AddSingleton<IReportExporter, Services.Reporting.ReportExporter>();

        return services;
    }
}
