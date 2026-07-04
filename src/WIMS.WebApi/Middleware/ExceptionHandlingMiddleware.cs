using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace WIMS.WebApi.Middleware;

/// <summary>معالجة موحّدة للأخطاء غير المتوقّعة → ProblemDetails بالعربية دون تسريب التفاصيل الحساسة.</summary>
public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            await WriteProblemAsync(context, StatusCodes.Status400BadRequest,
                "خطأ في التحقق", string.Join(" | ", ex.Errors.Select(e => e.ErrorMessage)));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "خطأ غير متوقّع أثناء معالجة {Path}", context.Request.Path);
            await WriteProblemAsync(context, StatusCodes.Status500InternalServerError,
                "خطأ داخلي", "حدث خطأ غير متوقّع. يُرجى المحاولة لاحقاً أو مراجعة الدعم الفني.");
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, int status, string title, string detail)
    {
        if (context.Response.HasStarted)
            return;

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path,
        };

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json; charset=utf-8";
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }
}
