using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WIMS.Application.Features.Auth.Login;
using WIMS.WebApi.Common;

namespace WIMS.WebApi.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(ISender sender) : ControllerBase
{
    /// <summary>تسجيل الدخول — يعيد رمز JWT عند النجاح.</summary>
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new LoginCommand(request.UserName, request.Password), cancellationToken);
        return result.ToActionResult();
    }
}

/// <summary>نموذج طلب تسجيل الدخول.</summary>
public sealed record LoginRequest(string UserName, string Password);
