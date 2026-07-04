namespace WIMS.Shared.Mapping;

/// <summary>
/// أداة تعيين (Mapping) خفيفة بديلة عن AutoMapper — تعتمد على مطابقة الأسماء (Convention-based)
/// مع تخزين مؤقت لخرائط الخصائص. تُسجَّل في DI كـ Singleton.
/// </summary>
public interface ISimpleMapper
{
    /// <summary>ينشئ كائن وجهة جديداً وينسخ إليه الخصائص المتطابقة اسماً ونوعاً من المصدر.</summary>
    TDestination Map<TDestination>(object source) where TDestination : new();

    /// <summary>نسخة مُحدَّدة النوعين للمصدر والوجهة.</summary>
    TDestination Map<TSource, TDestination>(TSource source) where TDestination : new();

    /// <summary>ينسخ الخصائص المتطابقة إلى كائن وجهة قائم (تحديث جزئي).</summary>
    void Map<TSource, TDestination>(TSource source, TDestination destination);

    /// <summary>تعيين قائمة كاملة.</summary>
    IReadOnlyList<TDestination> MapList<TDestination>(IEnumerable<object> source) where TDestination : new();
}
