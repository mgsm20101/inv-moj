using FluentValidation;
using MediatR;
using WIMS.Shared.Results;

namespace WIMS.Application.Common.Behaviors;

/// <summary>
/// يشغّل كل مُتحقّقات FluentValidation للطلب قبل تنفيذ الـ Handler.
/// عند وجود أخطاء وكان نوع الاستجابة <see cref="Result"/> يُرجِع فشلاً بدل رمي استثناء (أخطاء متوقعة)،
/// وإلا يرمي <see cref="ValidationException"/>.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var failures = validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0)
            return await next();

        var firstError = Error.Validation(
            failures[0].PropertyName,
            string.Join(" | ", failures.Select(f => f.ErrorMessage)));

        // إن كانت الاستجابة من نوع Result نُعيد فشلاً موحّداً بدل الاستثناء.
        if (TryCreateFailureResult(firstError, out var result))
            return result!;

        throw new ValidationException(failures);
    }

    private static bool TryCreateFailureResult(Error error, out TResponse? response)
    {
        response = default;
        var responseType = typeof(TResponse);

        if (responseType == typeof(Result))
        {
            response = (TResponse)(object)Result.Failure(error);
            return true;
        }

        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var valueType = responseType.GetGenericArguments()[0];
            var failureMethod = typeof(Result)
                .GetMethod(nameof(Result.Failure), 1, [typeof(Error)])!
                .MakeGenericMethod(valueType);
            response = (TResponse)failureMethod.Invoke(null, [error])!;
            return true;
        }

        return false;
    }
}
