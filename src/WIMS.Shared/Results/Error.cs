namespace WIMS.Shared.Results;

/// <summary>
/// يمثّل خطأً متوقّعاً في طبقة الأعمال (مثل: فشل تحقق، رصيد غير كافٍ، عدم وجود صلاحية).
/// يُستخدم مع <see cref="Result"/> بدل رمي الاستثناءات للأخطاء المتوقعة.
/// </summary>
public sealed record Error(string Code, string Message, ErrorType Type = ErrorType.Failure)
{
    /// <summary>لا يوجد خطأ.</summary>
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);

    /// <summary>خطأ تحقق (Validation) — يُرجّم عادةً إلى 400.</summary>
    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);

    /// <summary>عنصر غير موجود — يُرجّم إلى 404.</summary>
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);

    /// <summary>تعارض في الحالة (مثل تكرار) — يُرجّم إلى 409.</summary>
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);

    /// <summary>ممنوع بسبب الصلاحيات — يُرجّم إلى 403.</summary>
    public static Error Forbidden(string code, string message) => new(code, message, ErrorType.Forbidden);

    /// <summary>غير مصرّح (مصادقة) — يُرجّم إلى 401.</summary>
    public static Error Unauthorized(string code, string message) => new(code, message, ErrorType.Unauthorized);
}

/// <summary>تصنيف الخطأ لتحديد رمز HTTP المناسب في الطبقة الخارجية.</summary>
public enum ErrorType
{
    Failure = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Forbidden = 4,
    Unauthorized = 5
}
