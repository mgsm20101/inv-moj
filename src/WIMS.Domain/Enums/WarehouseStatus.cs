namespace WIMS.Domain.Enums;

/// <summary>حالة المخزن — تتحكّم بالسماح بالحركة (BR-03).</summary>
public enum WarehouseStatus : byte
{
    /// <summary>نشط — يسمح بالإدخال والصرف.</summary>
    Active = 1,

    /// <summary>مغلق — لا إدخال ولا صرف.</summary>
    Closed = 2,

    /// <summary>مجمّد — عرض وجرد فقط (يُستخدم أثناء الجرد).</summary>
    Frozen = 3,
}
