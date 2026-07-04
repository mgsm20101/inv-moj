using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace WIMS.IntegrationTests;

/// <summary>
/// اختبارات تكاملية لإدارة الصلاحيات (RBAC): إدارة المستخدمين/الأدوار وفرض الصلاحيات فعلياً.
/// تعتمد على المستخدمين المبذورين: admin (كل الصلاحيات) و finance (بلا Users.View/Roles.Manage).
/// </summary>
public sealed class AdminRbacTests(WimsWebAppFactory factory) : IClassFixture<WimsWebAppFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private async Task<HttpClient> AuthedClientAsync(string user, string password)
    {
        var client = factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/auth/login", new { userName = user, password });
        res.EnsureSuccessStatusCode();
        var payload = await res.Content.ReadFromJsonAsync<JsonElement>(Json);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", payload.GetProperty("token").GetString());
        return client;
    }

    [Fact]
    public async Task Admin_CanListUsers_200()
    {
        var client = await AuthedClientAsync("admin", "Admin@12345");
        var res = await client.GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var users = await res.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.True(users.GetArrayLength() > 0);
    }

    [Fact]
    public async Task NonAdmin_ListUsers_Returns403()
    {
        // finance يملك صلاحيات الموافقة فقط — لا Users.View.
        var client = await AuthedClientAsync("finance", "Finance@12345");
        var res = await client.GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Admin_GetPermissions_ReturnsCatalog()
    {
        var client = await AuthedClientAsync("admin", "Admin@12345");
        var res = await client.GetAsync("/api/permissions");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var perms = await res.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.True(perms.GetArrayLength() >= 30);
    }

    [Fact]
    public async Task Admin_CreateAndDeleteRole_Succeeds()
    {
        var client = await AuthedClientAsync("admin", "Admin@12345");
        var roleName = "اختبار-" + Guid.NewGuid().ToString("N")[..8];

        var create = await client.PostAsJsonAsync("/api/roles", new
        {
            name = roleName,
            description = "دور اختبار مؤقت",
            permissionKeys = new[] { "Items.View", "Dashboard.View" },
        });
        Assert.Equal(HttpStatusCode.OK, create.StatusCode);
        var roleId = (await create.Content.ReadFromJsonAsync<JsonElement>(Json)).GetGuid();

        // التحقق من حفظ الصلاحيات.
        var detail = await client.GetFromJsonAsync<JsonElement>($"/api/roles/{roleId}", Json);
        Assert.Equal(2, detail.GetProperty("permissionKeys").GetArrayLength());

        // تنظيف.
        var delete = await client.DeleteAsync($"/api/roles/{roleId}");
        Assert.Equal(HttpStatusCode.OK, delete.StatusCode);
    }

    [Fact]
    public async Task Admin_CannotDeleteSystemAdminRole()
    {
        var client = await AuthedClientAsync("admin", "Admin@12345");
        var roles = await client.GetFromJsonAsync<JsonElement>("/api/roles", Json);

        Guid adminRoleId = default;
        foreach (var r in roles.EnumerateArray())
            if (r.GetProperty("name").GetString() == "مدير النظام")
                adminRoleId = r.GetProperty("id").GetGuid();

        Assert.NotEqual(default, adminRoleId);
        var delete = await client.DeleteAsync($"/api/roles/{adminRoleId}");
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }
}
