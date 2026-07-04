namespace WIMS.Application.Common.Interfaces;

/// <summary>يولّد رمز JWT للمستخدم بعد نجاح المصادقة.</summary>
public interface IJwtTokenService
{
    /// <summary>ينشئ رمز وصول (Access Token) يتضمّن الأدوار والصلاحيات.</summary>
    (string Token, DateTime ExpiresAtUtc) CreateToken(
        string userId,
        string userName,
        IEnumerable<string> roles,
        IEnumerable<string> permissions);
}
