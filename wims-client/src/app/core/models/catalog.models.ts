// نماذج الكتالوج — مطابقة لعقود WIMS.Application DTOs.

export enum ItemType {
  Consumable = 1,
  Durable = 2,
  Hazardous = 3,
  Perishable = 4,
}

export const ITEM_TYPE_LABEL: Record<ItemType, string> = {
  [ItemType.Consumable]: 'مستهلك',
  [ItemType.Durable]: 'مستديم',
  [ItemType.Hazardous]: 'خطر',
  [ItemType.Perishable]: 'قابل للتلف',
};

/** مطابق لـ ItemDto (GetItems / GetItemById). */
export interface ItemDto {
  id: string;
  itemCode: string;
  barcode: string | null;
  nameAr: string;
  nameEn: string | null;
  description: string | null;
  categoryId: string;
  categoryName: string;
  itemType: ItemType;
  baseUnitId: string;
  baseUnitName: string;
  tracksBatch: boolean;
  tracksExpiry: boolean;
  tracksSerial: boolean;
  minStock: number;
  maxStock: number | null;
  reorderPoint: number;
  weightedAvgCost: number;
  hazardClass: string | null;
  isActive: boolean;
  isStockItem: boolean;
}

/** مطابق لـ PagedResult<T>. */
export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface ItemsQuery {
  search?: string;
  categoryId?: string;
  isActive?: boolean;
  page: number;
  pageSize: number;
}

/** مطابق لـ CategoryDto. */
export interface CategoryDto {
  id: string;
  code: string;
  nameAr: string;
  nameEn: string | null;
  parentId: string | null;
  level: number;
  path: string;
  isActive: boolean;
  isLeaf: boolean;
}

/** مطابق لـ UnitDto. */
export interface UnitDto {
  id: string;
  code: string;
  nameAr: string;
  isBaseUnit: boolean;
  isActive: boolean;
}

// ---- الاستيراد (Import) ----

/** مطابق لـ ImportRowError. RowNumber صفري. */
export interface ImportRowError {
  rowNumber: number;
  column: string;
  message: string;
}

/** مطابق لـ ImportResult. */
export interface ImportResult {
  totalRows: number;
  validRows: number;
  importedCount: number;
  committed: boolean;
  errors: ImportRowError[];
  hasErrors: boolean;
}

/** غلاف الخطأ من ApiResults (ErrorType → HTTP status). */
export interface ApiError {
  code: string;
  message: string;
}
