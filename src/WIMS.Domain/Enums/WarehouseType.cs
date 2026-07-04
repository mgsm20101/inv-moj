namespace WIMS.Domain.Enums;

/// <summary>نوع المخزن.</summary>
public enum WarehouseType : byte
{
    /// <summary>رئيسي.</summary>
    Main = 1,

    /// <summary>فرعي.</summary>
    Sub = 2,

    /// <summary>عهدة.</summary>
    Custody = 3,

    /// <summary>تالف / إتلاف.</summary>
    Damaged = 4,

    /// <summary>حجر / Quarantine.</summary>
    Quarantine = 5,
}
