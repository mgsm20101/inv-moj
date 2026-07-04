using WIMS.Application.Common.Messaging;
using WIMS.Shared.Results;

namespace WIMS.Application.Features.Auth.Login;

/// <summary>أمر تسجيل الدخول — يتحقق من البيانات ويُصدر رمز JWT.</summary>
public sealed record LoginCommand(string UserName, string Password) : ICommand<Result<LoginResult>>;

/// <summary>نتيجة تسجيل الدخول الناجح.</summary>
public sealed record LoginResult(
    string Token,
    DateTime ExpiresAtUtc,
    string UserName,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);
