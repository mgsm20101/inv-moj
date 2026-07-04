using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace WIMS.IntegrationTests;

/// <summary>يتحقق أن مُحدِّد المعدّل على تسجيل الدخول يردّ 429 بعد تجاوز الحد (OWASP A07).</summary>
public sealed class RateLimitingTests
{
    [Fact]
    public async Task Login_ExceedingLimit_Returns429()
    {
        // فعّل المُحدِّد بحدّ منخفض عبر متغيّرات البيئة (يقرأها CreateBuilder فوراً)،
        // ثم أقلِع مضيفاً جديداً. الاختبارات متسلسلة فتُستعاد القيم بأمان.
        Environment.SetEnvironmentVariable("RateLimiting__Enabled", "true");
        Environment.SetEnvironmentVariable("RateLimiting__Login__PermitPerMinute", "3");
        try
        {
            await using var factory = new WebApplicationFactory<Program>();
            var client = factory.CreateClient();

            // مستخدم غير موجود (لا "admin") — يتحقّق الاختبار من حدّ المعدّل بمعزل عن
            // سياسة القفل بعد المحاولات الفاشلة، فلا يُسمَّم حساب admin المشترك بين الاختبارات.
            var statuses = new List<HttpStatusCode>();
            for (var i = 0; i < 6; i++)
            {
                var res = await client.PostAsJsonAsync("/api/auth/login",
                    new { userName = "rate-limit-probe", password = "WRONG_password_1" });
                statuses.Add(res.StatusCode);
            }

            // أول ~3 محاولات ضمن الحد (401)، ثم يبدأ الرفض 429.
            Assert.Contains(HttpStatusCode.TooManyRequests, statuses);
        }
        finally
        {
            Environment.SetEnvironmentVariable("RateLimiting__Enabled", "false");
            Environment.SetEnvironmentVariable("RateLimiting__Login__PermitPerMinute", null);
        }
    }
}
