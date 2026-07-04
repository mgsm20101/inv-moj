using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using WIMS.Application.Common.Interfaces;

namespace WIMS.Application.Common.Behaviors;

/// <summary>يسجّل بداية/نهاية كل طلب وزمن تنفيذه والمستخدم المنفِّذ عبر Serilog.</summary>
public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger,
    ICurrentUser currentUser)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var user = currentUser.UserName ?? "مجهول";
        var stopwatch = Stopwatch.StartNew();

        logger.LogInformation("بدء {RequestName} بواسطة {User}", requestName, user);
        try
        {
            var response = await next();
            stopwatch.Stop();
            logger.LogInformation("اكتمل {RequestName} في {Elapsed} ملّي ثانية", requestName, stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "فشل {RequestName} بعد {Elapsed} ملّي ثانية", requestName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
