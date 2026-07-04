namespace WIMS.Domain.Enums;

/// <summary>نوع الموقع داخل المخزن — يُطابَق مع نوع الصنف (BR-05).</summary>
public enum LocationType : byte
{
    /// <summary>عادي.</summary>
    Standard = 1,

    /// <summary>مبرّد.</summary>
    Cold = 2,

    /// <summary>خطر.</summary>
    Hazard = 3,

    /// <summary>حجر.</summary>
    Quarantine = 4,
}
