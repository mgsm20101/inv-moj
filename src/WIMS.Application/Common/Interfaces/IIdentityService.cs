using WIMS.Shared.Results;

namespace WIMS.Application.Common.Interfaces;

/// <summary>تجريد عمليات المصادقة والهوية (ASP.NET Core Identity) لطبقة Application.</summary>
public interface IIdentityService
{
    /// <summary>يتحقق من بيانات الدخول ويعيد بيانات المصادقة عند النجاح.</summary>
    Task<Result<AuthenticatedUser>> ValidateCredentialsAsync(
        string userName, string password, CancellationToken cancellationToken = default);
}

/// <summary>بيانات المستخدم المُصادَق عليه — تُستخدم لبناء الرمز.</summary>
public sealed record AuthenticatedUser(
    string UserId,
    string UserName,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);
