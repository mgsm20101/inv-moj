namespace WIMS.Infrastructure.Identity;

/// <summary>إعدادات رمز JWT — تُقرأ من قسم "Jwt" في appsettings.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SigningKey { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 60;
}
