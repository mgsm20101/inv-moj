using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace WIMS.IntegrationTests;

/// <summary>
/// اختبارات تكاملية end-to-end للمصادقة والصلاحيات والتصلّب الأمني (المرحلة 6).
/// تُقلع التطبيق كاملاً على قاعدة WIMS_Test وتتحقق عبر HTTP حقيقي.
/// </summary>
public sealed class AuthAndSecurityTests(WimsWebAppFactory factory) : IClassFixture<WimsWebAppFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private async Task<string> LoginAsync(HttpClient client, string user = "admin", string password = "Admin@12345")
    {
        var res = await client.PostAsJsonAsync("/api/auth/login", new { userName = user, password });
        res.EnsureSuccessStatusCode();
        var payload = await res.Content.ReadFromJsonAsync<JsonElement>(Json);
        return payload.GetProperty("token").GetString()!;
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokenAndPermissions()
    {
        var client = factory.CreateClient();

        var res = await client.PostAsJsonAsync("/api/auth/login",
            new { userName = "admin", password = "Admin@12345" });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var payload = await res.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.False(string.IsNullOrWhiteSpace(payload.GetProperty("token").GetString()));
        Assert.True(payload.GetProperty("permissions").GetArrayLength() > 0);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var client = factory.CreateClient();

        var res = await client.PostAsJsonAsync("/api/auth/login",
            new { userName = "admin", password = "WRONG_password_1" });

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        var client = factory.CreateClient();

        var res = await client.GetAsync("/api/dashboard");

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithToken_Returns200()
    {
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var res = await client.GetAsync("/api/dashboard");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Report_StockBalance_WithToken_ReturnsPdf()
    {
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var res = await client.GetAsync("/api/reports/stock-balance");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal("application/pdf", res.Content.Headers.ContentType?.MediaType);
        var bytes = await res.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 4);
        Assert.Equal("%PDF-", System.Text.Encoding.ASCII.GetString(bytes, 0, 5));
    }

    [Fact]
    public async Task Responses_IncludeSecurityHeaders()
    {
        var client = factory.CreateClient();

        var res = await client.GetAsync("/api/dashboard"); // 401 لكن الترويسات تُضاف للجميع

        Assert.Equal("nosniff", res.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", res.Headers.GetValues("X-Frame-Options").Single());
    }
}
