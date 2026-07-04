namespace WIMS.Domain.Enums;

/// <summary>حالة الموظف.</summary>
public enum EmployeeStatus : byte
{
    Active = 1,       // نشط
    Suspended = 2,    // موقوف
    Transferred = 3,  // منقول
    Terminated = 4,   // منتهي الخدمة
}

/// <summary>نوع العهدة.</summary>
public enum CustodyType : byte
{
    Personal = 1,   // شخصية (على موظف)
    Warehouse = 2,  // مخزنية (على أمين مخزن)
}

/// <summary>حالة ملف العهدة.</summary>
public enum CustodyStatus : byte
{
    Active = 1,       // نشطة
    Cleared = 2,      // مُخلاة (براءة ذمة)
    Transferred = 3,  // منقولة
}

/// <summary>حالة بند العهدة.</summary>
public enum CustodyItemStatus : byte
{
    InCustody = 1,    // في العهدة
    Returned = 2,     // أُرجع للمخزن
    Transferred = 3,  // نُقل لموظف آخر
    WrittenOff = 4,   // أُعدم/شُطب
}

/// <summary>هدف مسار الموافقة.</summary>
public enum ApprovalTargetType : byte
{
    Voucher = 1,
    CustodyTransfer = 2,
    Clearance = 3,
}

/// <summary>حالة طلب الموافقة الحيّ.</summary>
public enum ApprovalRequestStatus : byte
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    ReturnedForRevision = 4,
    Cancelled = 5,
}

/// <summary>نوع إجراء الموافقة.</summary>
public enum ApprovalActionType : byte
{
    Approve = 1,
    Reject = 2,
    ReturnForRevision = 3,
    Delegate = 4,
}
