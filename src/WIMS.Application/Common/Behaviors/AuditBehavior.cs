using MediatR;
using WIMS.Application.Common.Interfaces;
using WIMS.Application.Common.Messaging;
using WIMS.Domain.Auditing;
using WIMS.Shared.Results;

namespace WIMS.Application.Common.Behaviors;

/// <summary>
/// يكتب سجل تدقيق (AuditLog) آلياً لكل أمر (ICommand) بعد نجاح تنفيذه — سلوك افتراضي
/// بدل التدقيق اليدوي داخل كل Handler (حسب القسم 7 من الخطة). الاستعلامات لا تُدقَّق.
/// </summary>
public sealed class AuditBehavior<TRequest, TResponse>(
    ICurrentUser currentUser,
    IAppDbContext dbContext)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next();

        // لا تُسجَّل التدقيق ولا يُحفَظ أي شيء عند فشل الأمر: بعض المعالجات (مثل اعتماد
        // السندات) تُغيّر كيانات متتبَّعة قبل فحصٍ لاحق قد يفشل ويُرجِع بلا استدعاء
        // SaveChanges — استدعاء SaveChanges هنا بلا شرط كان يُثبِّت تلك التغييرات الجزئية
        // رغم فشل العملية فعلياً (حالة بيانات غير متّسقة صامتة).
        if (request is IBaseCommand && response is Result { IsSuccess: true })
        {
            dbContext.AuditLogs.Add(new AuditLog
            {
                UserId = currentUser.UserId,
                UserName = currentUser.UserName,
                Timestamp = DateTime.UtcNow,
                Entity = typeof(TRequest).Name,
                Action = ResolveAction(typeof(TRequest).Name),
            });

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return response;
    }

    private static string ResolveAction(string requestName)
    {
        if (requestName.StartsWith("Create", StringComparison.OrdinalIgnoreCase)) return "Create";
        if (requestName.StartsWith("Update", StringComparison.OrdinalIgnoreCase)) return "Update";
        if (requestName.StartsWith("Delete", StringComparison.OrdinalIgnoreCase)
            || requestName.StartsWith("Deactivate", StringComparison.OrdinalIgnoreCase)) return "Delete";
        if (requestName.StartsWith("Login", StringComparison.OrdinalIgnoreCase)) return "Login";
        return "Execute";
    }
}
