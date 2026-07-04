using System.Collections.Concurrent;
using System.Reflection;

namespace WIMS.Shared.Mapping;

/// <summary>
/// تنفيذ <see cref="ISimpleMapper"/> بالاعتماد على الـ Reflection مع تخزين مؤقت (Cache)
/// لأزواج الخصائص لكل (نوع مصدر، نوع وجهة) لتقليل كلفة الأداء.
/// المطابقة: نفس الاسم (غير حسّاس لحالة الأحرف) ونوع الوجهة يقبل نوع المصدر.
/// </summary>
public sealed class SimpleMapper : ISimpleMapper
{
    private static readonly ConcurrentDictionary<(Type Source, Type Dest), PropertyPair[]> Cache = new();

    private readonly record struct PropertyPair(PropertyInfo Source, PropertyInfo Dest);

    public TDestination Map<TDestination>(object source) where TDestination : new()
    {
        ArgumentNullException.ThrowIfNull(source);
        var destination = new TDestination();
        Copy(source, destination);
        return destination;
    }

    public TDestination Map<TSource, TDestination>(TSource source) where TDestination : new()
    {
        ArgumentNullException.ThrowIfNull(source);
        var destination = new TDestination();
        Copy(source, destination);
        return destination;
    }

    public void Map<TSource, TDestination>(TSource source, TDestination destination)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        Copy(source, destination);
    }

    public IReadOnlyList<TDestination> MapList<TDestination>(IEnumerable<object> source) where TDestination : new()
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.Select(Map<TDestination>).ToList();
    }

    private static void Copy(object source, object destination)
    {
        var pairs = Cache.GetOrAdd(
            (source.GetType(), destination.GetType()),
            static key => BuildPairs(key.Source, key.Dest));

        foreach (var pair in pairs)
        {
            var value = pair.Source.GetValue(source);
            pair.Dest.SetValue(destination, value);
        }
    }

    private static PropertyPair[] BuildPairs(Type sourceType, Type destType)
    {
        var destProps = destType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite)
            .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

        var pairs = new List<PropertyPair>();
        foreach (var srcProp in sourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!srcProp.CanRead) continue;
            if (destProps.TryGetValue(srcProp.Name, out var destProp)
                && destProp.PropertyType.IsAssignableFrom(srcProp.PropertyType))
            {
                pairs.Add(new PropertyPair(srcProp, destProp));
            }
        }

        return pairs.ToArray();
    }
}
