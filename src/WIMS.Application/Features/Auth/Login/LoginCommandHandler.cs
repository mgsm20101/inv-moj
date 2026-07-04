using MediatR;
using WIMS.Application.Common.Interfaces;
using WIMS.Shared.Results;

namespace WIMS.Application.Features.Auth.Login;

public sealed class LoginCommandHandler(
    IIdentityService identityService,
    IJwtTokenService jwtTokenService)
    : IRequestHandler<LoginCommand, Result<LoginResult>>
{
    public async Task<Result<LoginResult>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var validation = await identityService.ValidateCredentialsAsync(
            request.UserName, request.Password, cancellationToken);

        if (validation.IsFailure)
            return Result.Failure<LoginResult>(validation.Error);

        var user = validation.Value;
        var (token, expiresAt) = jwtTokenService.CreateToken(
            user.UserId, user.UserName, user.Roles, user.Permissions);

        return new LoginResult(token, expiresAt, user.UserName, user.Roles, user.Permissions);
    }
}
