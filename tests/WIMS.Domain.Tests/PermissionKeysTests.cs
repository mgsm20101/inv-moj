using System.Reflection;
using WIMS.Domain.Authorization;

namespace WIMS.Domain.Tests;

/// <summary>
/// يحرس ثبات مصدر الصلاحيات الوحيد (<see cref="PermissionKeys"/>): لا تكرار،
/// وكل ثابت مفتاح معرّف في فئة متداخلة مُدرَج في قائمة البذر <c>All</c> والعكس.
/// كسر هذا الثبات = صلاحية تُفرَض في [Authorize] لكنها لا تُبذَر (أو العكس).
/// </summary>
public class PermissionKeysTests
{
    /// <summary>كل ثوابت المفاتيح المعرّفة داخل الفئات المتداخلة عبر الـ reflection.</summary>
    private static IReadOnlyList<string> DeclaredKeys()
    {
        var keys = new List<string>();
        foreach (var nested in typeof(PermissionKeys).GetNestedTypes(BindingFlags.Public | BindingFlags.Static))
        {
            foreach (var field in nested.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (field is { IsLiteral: true, IsInitOnly: false } && field.FieldType == typeof(string))
                    keys.Add((string)field.GetRawConstantValue()!);
            }
        }
        return keys;
    }

    [Fact]
    public void All_HasNoDuplicateKeys()
    {
        var keys = PermissionKeys.All.Select(p => p.Key).ToList();
        Assert.Equal(keys.Count, keys.Distinct().Count());
    }

    [Fact]
    public void EveryDeclaredKey_IsSeededInAll()
    {
        var seeded = PermissionKeys.All.Select(p => p.Key).ToHashSet();
        var missing = DeclaredKeys().Where(k => !seeded.Contains(k)).ToList();
        Assert.True(missing.Count == 0, $"مفاتيح معرّفة لكنها غير مبذورة: {string.Join(", ", missing)}");
    }

    [Fact]
    public void EverySeededKey_IsDeclaredConstant()
    {
        var declared = DeclaredKeys().ToHashSet();
        var extra = PermissionKeys.All.Select(p => p.Key).Where(k => !declared.Contains(k)).ToList();
        Assert.True(extra.Count == 0, $"مفاتيح مبذورة بلا ثابت مقابل: {string.Join(", ", extra)}");
    }

    [Fact]
    public void All_EntriesHaveNameAndModule()
    {
        Assert.All(PermissionKeys.All, p =>
        {
            Assert.False(string.IsNullOrWhiteSpace(p.Key));
            Assert.False(string.IsNullOrWhiteSpace(p.Name));
            Assert.False(string.IsNullOrWhiteSpace(p.Module));
        });
    }
}
