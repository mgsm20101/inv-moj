namespace WIMS.Application.Common.Interfaces;

/// <summary>يوفّر معلومات المستخدم الحالي من سياق الطلب (HttpContext) لطبقة Application.</summary>
public interface ICurrentUser
{
    string? UserId { get; }
    string? UserName { get; }
    bool IsAuthenticated { get; }
    bool HasPermission(string permissionKey);

    /// <summary>هل ينتمي المستخدم الحالي للدور المحدّد؟ (يُستخدم في بوابة اعتماد الموافقات حسب الدور).</summary>
    bool IsInRole(string role);
}
