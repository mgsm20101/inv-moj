using System.Runtime.CompilerServices;

namespace WIMS.Shared.Guards;

/// <summary>حراسات (Guard Clauses) للتحقق من الوسائط في بداية الدوال.</summary>
public static class Guard
{
    public static T AgainstNull<T>(T? value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : class
        => value ?? throw new ArgumentNullException(paramName);

    public static string AgainstNullOrWhiteSpace(string? value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        => string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("القيمة لا يمكن أن تكون فارغة.", paramName)
            : value;

    public static int AgainstNegative(int value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        => value < 0 ? throw new ArgumentOutOfRangeException(paramName, "القيمة لا يمكن أن تكون سالبة.") : value;

    public static decimal AgainstNegative(decimal value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        => value < 0 ? throw new ArgumentOutOfRangeException(paramName, "القيمة لا يمكن أن تكون سالبة.") : value;

    public static decimal AgainstNonPositive(decimal value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        => value <= 0 ? throw new ArgumentOutOfRangeException(paramName, "القيمة يجب أن تكون أكبر من صفر.") : value;

    public static Guid AgainstEmpty(Guid value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        => value == Guid.Empty ? throw new ArgumentException("المعرّف لا يمكن أن يكون فارغاً.", paramName) : value;
}
