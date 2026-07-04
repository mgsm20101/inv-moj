using WIMS.Domain.Common;

namespace WIMS.Domain.Catalog;

/// <summary>تصنيف شجري للأصناف (Hierarchical). الأصناف تُعلَّق على العُقد الورقية فقط (BR-09).</summary>
public class ItemCategory : BaseEntity
{
    /// <summary>كود التصنيف — جزء من كود الصنف الموحّد. مثال: "01"، "0103".</summary>
    public string Code { get; set; } = string.Empty;

    public string NameAr { get; set; } = string.Empty;
    public string? NameEn { get; set; }

    /// <summary>التصنيف الأب (null = جذر).</summary>
    public Guid? ParentId { get; set; }
    public ItemCategory? Parent { get; set; }
    public ICollection<ItemCategory> Children { get; set; } = new List<ItemCategory>();

    /// <summary>عمق العقدة (1=رئيسي). يُحسب تلقائياً.</summary>
    public byte Level { get; set; } = 1;

    /// <summary>مسار مادّي "01/0103/010305" لتسريع الاستعلامات الشجرية.</summary>
    public string Path { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    public ICollection<Item> Items { get; set; } = new List<Item>();
}
