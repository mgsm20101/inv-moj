namespace WIMS.Domain.Enums;

/// <summary>نوع الصنف — يحدّد سلوك التتبّع والتخزين.</summary>
public enum ItemType : byte
{
    /// <summary>مستهلك.</summary>
    Consumable = 1,

    /// <summary>مستديم (أصل ثابت يُتتبّع بسيريال ويتحوّل لعهدة).</summary>
    Durable = 2,

    /// <summary>خطر (يتطلب تصنيف خطورة وموقعاً متوافقاً).</summary>
    Hazardous = 3,

    /// <summary>قابل للتلف (يتطلب تاريخ صلاحية وصرف FEFO).</summary>
    Perishable = 4,
}
