using Microsoft.AspNetCore.Identity;

namespace WIMS.Infrastructure.Identity;

/// <summary>مستخدم النظام — يمتد من ASP.NET Core Identity بمفتاح Guid.</summary>
public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>صورة المستخدم (اختيارية) — مُخزَّنة في قاعدة البيانات (varbinary).</summary>
    public byte[]? PhotoData { get; set; }
    public string? PhotoContentType { get; set; }
}
