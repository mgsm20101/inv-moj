using Microsoft.AspNetCore.Mvc;
using WIMS.Shared.Results;

namespace WIMS.WebApi.Common;

/// <summary>يحوّل <see cref="Result"/> إلى استجابة HTTP مناسبة حسب نوع الخطأ.</summary>
public static class ApiResults
{
    public static IActionResult ToActionResult<T>(this Result<T> result)
        => result.IsSuccess
            ? new OkObjectResult(result.Value)
            : Problem(result.Error);

    public static IActionResult ToActionResult(this Result result)
        => result.IsSuccess
            ? new OkResult()
            : Problem(result.Error);

    private static IActionResult Problem(Error error)
    {
        var status = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest,
        };

        var problem = new ProblemDetails
        {
            Status = status,
            Title = error.Code,
            Detail = error.Message,
        };

        return new ObjectResult(problem) { StatusCode = status };
    }
}
