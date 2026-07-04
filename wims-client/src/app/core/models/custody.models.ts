// نماذج المرحلة ٣: العُهد (Custody) — مطابقة لعقود الـ backend الفعلية.
// المصادر: WIMS.Domain/Enums/CustodyApprovalEnums.cs،
//          WIMS.Application/Features/Custody/CustodyFeatures.cs،
//          WIMS.Application/Features/Employees/EmployeeFeatures.cs

/** حالة الموظف — EmployeeStatus (byte). */
export enum EmployeeStatus {
  Active = 1,
  Suspended = 2,
  Transferred = 3,
  Terminated = 4,
}

export const EMPLOYEE_STATUS_LABEL: Record<EmployeeStatus, string> = {
  [EmployeeStatus.Active]: 'على رأس العمل',
  [EmployeeStatus.Suspended]: 'موقوف',
  [EmployeeStatus.Transferred]: 'منقول',
  [EmployeeStatus.Terminated]: 'منتهية خدمته',
};

/** تعيين حالة الموظف إلى صنف الـ badge (أيقونة+نص+لون، ليس اللون وحده). */
export const EMPLOYEE_STATUS_BADGE: Record<EmployeeStatus, string> = {
  [EmployeeStatus.Active]: 'badge--approved',
  [EmployeeStatus.Suspended]: 'badge--pending',
  [EmployeeStatus.Transferred]: 'badge--draft',
  [EmployeeStatus.Terminated]: 'badge--rejected',
};

/** حالة بند العهدة — CustodyItemStatus (byte). */
export enum CustodyItemStatus {
  InCustody = 1,
  Returned = 2,
  Transferred = 3,
  WrittenOff = 4,
}

export const CUSTODY_ITEM_STATUS_LABEL: Record<CustodyItemStatus, string> = {
  [CustodyItemStatus.InCustody]: 'في العهدة',
  [CustodyItemStatus.Returned]: 'مُرتجع',
  [CustodyItemStatus.Transferred]: 'منقول',
  [CustodyItemStatus.WrittenOff]: 'مُستبعد',
};

export const CUSTODY_ITEM_STATUS_BADGE: Record<CustodyItemStatus, string> = {
  [CustodyItemStatus.InCustody]: 'badge--pending',
  [CustodyItemStatus.Returned]: 'badge--approved',
  [CustodyItemStatus.Transferred]: 'badge--draft',
  [CustodyItemStatus.WrittenOff]: 'badge--rejected',
};

/** موظف — GET /api/employees (قائمة خام، غير مصفّحة). */
export interface EmployeeDto {
  id: string;
  employeeNo: string;
  nationalId: string;
  fullNameAr: string;
  department: string;
  email: string | null;
  status: EmployeeStatus;
}

/** بند كشف عهدة — CustodyItemLineDto. */
export interface CustodyItemLineDto {
  custodyItemId: string;
  itemCode: string;
  itemName: string;
  serialNo: string | null;
  qty: number;
  unitCost: number;
  value: number;
  assignedAt: string;
  status: CustodyItemStatus;
}

/** كشف عهدة موظف — CustodyStatementDto (مغلّف في Result على الخادم، مُفكّك عبر ToActionResult). */
export interface CustodyStatementDto {
  employeeId: string;
  employeeNo: string;
  employeeName: string;
  employeeStatus: EmployeeStatus;
  itemCount: number;
  totalValue: number;
  items: CustodyItemLineDto[];
}
