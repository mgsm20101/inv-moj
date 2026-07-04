using WIMS.Domain.Common;

namespace WIMS.Domain.Catalog;

/// <summary>وحدة القياس. المرحلة 1: وحدة أساس واحدة لكل صنف (بلا عوامل تحويل).</summary>
public class UnitOfMeasure : BaseEntity
{
    /// <summary>رمز الوحدة: "PCS"، "BOX"، "KG"، "LTR"، "REAM".</summary>
    public string Code { get; set; } = string.Empty;

    public string NameAr { get; set; } = string.Empty;

    /// <summary>هل هي وحدة أساس تُخزَّن بها الأرصدة.</summary>
    public bool IsBaseUnit { get; set; } = true;

    public bool IsActive { get; set; } = true;

    public ICollection<Item> Items { get; set; } = new List<Item>();
}
