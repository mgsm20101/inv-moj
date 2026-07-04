// نماذج شاشة «البيانات الأساسية» — طلبات الإنشاء/التعديل وتفاصيل التعديل،
// مطابقة لعقود الـ backend (Warehouses / Employees / Categories / Units Features).

// ---- المخازن ----

/** WarehouseType (byte) — مطابق لـ WIMS.Domain.Enums.WarehouseType. */
export enum WarehouseType {
  Main = 1,
  Sub = 2,
  Custody = 3,
  Damaged = 4,
  Quarantine = 5,
}

export const WAREHOUSE_TYPE_LABEL: Record<WarehouseType, string> = {
  [WarehouseType.Main]: 'رئيسي',
  [WarehouseType.Sub]: 'فرعي',
  [WarehouseType.Custody]: 'عُهد',
  [WarehouseType.Damaged]: 'تالف',
  [WarehouseType.Quarantine]: 'حجر',
};

/** قائمة الأنواع للقائمة المنسدلة. */
export const WAREHOUSE_TYPES: { value: WarehouseType; label: string }[] = [
  { value: WarehouseType.Main, label: WAREHOUSE_TYPE_LABEL[WarehouseType.Main] },
  { value: WarehouseType.Sub, label: WAREHOUSE_TYPE_LABEL[WarehouseType.Sub] },
  { value: WarehouseType.Custody, label: WAREHOUSE_TYPE_LABEL[WarehouseType.Custody] },
  { value: WarehouseType.Damaged, label: WAREHOUSE_TYPE_LABEL[WarehouseType.Damaged] },
  { value: WarehouseType.Quarantine, label: WAREHOUSE_TYPE_LABEL[WarehouseType.Quarantine] },
];

/** مطابق لـ WarehouseDetailDto (GET /api/warehouses/{id}) — يشمل keeperUserId. */
export interface WarehouseDetailDto {
  id: string;
  code: string;
  nameAr: string;
  warehouseType: WarehouseType;
  region: string | null;
  keeperUserId: string;
  usesLocations: boolean;
  isActive: boolean;
}

export interface CreateWarehouseRequest {
  code: string;
  nameAr: string;
  warehouseType: WarehouseType;
  region?: string | null;
  keeperUserId: string;
  usesLocations: boolean;
}

export interface UpdateWarehouseRequest {
  nameAr: string;
  warehouseType: WarehouseType;
  region?: string | null;
  keeperUserId: string;
  usesLocations: boolean;
}

// ---- الموظفون ----

/** مطابق لـ EmployeeDetailDto (GET /api/employees/{id}). */
export interface EmployeeDetailDto {
  id: string;
  employeeNo: string;
  nationalId: string;
  fullNameAr: string;
  fullNameEn: string | null;
  department: string;
  jobTitle: string | null;
  email: string | null;
  phone: string | null;
  userId: string | null;
  status: number;
}

export interface CreateEmployeeRequest {
  employeeNo: string;
  nationalId: string;
  fullNameAr: string;
  fullNameEn?: string | null;
  department: string;
  jobTitle?: string | null;
  email?: string | null;
  phone?: string | null;
  userId?: string | null;
}

export interface UpdateEmployeeRequest {
  fullNameAr: string;
  fullNameEn?: string | null;
  department: string;
  jobTitle?: string | null;
  email?: string | null;
  phone?: string | null;
  userId?: string | null;
}

// ---- التصنيفات ----

export interface CreateCategoryRequest {
  code: string;
  nameAr: string;
  nameEn?: string | null;
  parentId?: string | null;
  sortOrder: number;
}

export interface UpdateCategoryRequest {
  nameAr: string;
  nameEn?: string | null;
  sortOrder: number;
}

// ---- وحدات القياس ----

export interface CreateUnitRequest {
  code: string;
  nameAr: string;
  isBaseUnit: boolean;
}

export interface UpdateUnitRequest {
  nameAr: string;
  isBaseUnit: boolean;
}
