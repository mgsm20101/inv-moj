using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc.Testing;

// اختبارات تكاملية تتشارك قاعدة WIMS_Test وحالة مُحدِّد المعدّل — عطّل التوازي لتفادي التسابق.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace WIMS.IntegrationTests;

/// <summary>
/// يضبط متغيّرات البيئة قبل تحميل أي مضيف. ضروري لأن <c>Program</c> يقرأ الإعدادات
/// مباشرةً وقت التهيئة (الحارس/المُحدِّد/سلسلة الاتصال) — لذا التجاوز عبر
/// ConfigureAppConfiguration يصل متأخّراً. متغيّرات البيئة يلتقطها CreateBuilder فوراً.
/// </summary>
internal static class TestEnvironment
{
    internal const string TestConnection =
        "Server=(localdb)\\MSSQLLocalDB;Database=WIMS_Test;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

    [ModuleInitializer]
    public static void Init()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", TestConnection);
        Environment.SetEnvironmentVariable("Jwt__SigningKey", "integration_test_signing_key_at_least_32_bytes_long_0");
        Environment.SetEnvironmentVariable("RateLimiting__Enabled", "false");
    }
}

/// <summary>مصنع تطبيق للاختبارات التكاملية عبر <see cref="WebApplicationFactory{Program}"/> على قاعدة WIMS_Test.</summary>
public class WimsWebAppFactory : WebApplicationFactory<Program>;
