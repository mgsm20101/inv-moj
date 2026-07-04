// نماذج الحركات/الأذون — مطابقة لعقود WIMS.Application (Phase 2).

export enum VoucherType {
  Receipt = 1,
  Issue = 2,
  Transfer = 3,
  Return = 4,
  Adjustment = 5,
  Reversal = 6,
}

export const VOUCHER_TYPE_LABEL: Record<VoucherType, string> = {
  [VoucherType.Receipt]: 'استلام',
  [VoucherType.Issue]: 'صرف',
  [VoucherType.Transfer]: 'تحويل',
  [VoucherType.Return]: 'مرتجع',
  [VoucherType.Adjustment]: 'تسوية',
  [VoucherType.Reversal]: 'عكس قيد',
};

export enum VoucherStatus {
  Draft = 1,
  UnderReview = 2,
  Approved = 3,
  Rejected = 4,
  Cancelled = 5,
}

export const VOUCHER_STATUS_LABEL: Record<VoucherStatus, string> = {
  [VoucherStatus.Draft]: 'مسوّدة',
  [VoucherStatus.UnderReview]: 'قيد الاعتماد',
  [VoucherStatus.Approved]: 'معتمَد',
  [VoucherStatus.Rejected]: 'مرفوض',
  [VoucherStatus.Cancelled]: 'ملغى',
};

/** فئة الـ badge المشتركة لكل حالة. */
export const VOUCHER_STATUS_BADGE: Record<VoucherStatus, string> = {
  [VoucherStatus.Draft]: 'badge--draft',
  [VoucherStatus.UnderReview]: 'badge--pending',
  [VoucherStatus.Approved]: 'badge--approved',
  [VoucherStatus.Rejected]: 'badge--rejected',
  [VoucherStatus.Cancelled]: 'badge--draft',
};

export enum AdjustmentType {
  IncreaseFound = 1,
  DecreaseShortage = 2,
  Damage = 3,
  Destruction = 4,
  Expiry = 5,
}

export const ADJUSTMENT_TYPE_LABEL: Record<AdjustmentType, string> = {
  [AdjustmentType.IncreaseFound]: 'زيادة/عثور',
  [AdjustmentType.DecreaseShortage]: 'نقص/عجز',
  [AdjustmentType.Damage]: 'تلف',
  [AdjustmentType.Destruction]: 'إتلاف',
  [AdjustmentType.Expiry]: 'انتهاء صلاحية',
};

export enum TransferStatus {
  Draft = 1,
  InTransit = 2,
  Received = 3,
}

export const TRANSFER_STATUS_LABEL: Record<TransferStatus, string> = {
  [TransferStatus.Draft]: 'مسوّدة',
  [TransferStatus.InTransit]: 'في الطريق',
  [TransferStatus.Received]: 'مستلَم',
};

/** صف قائمة الأذون (VoucherDto). */
export interface VoucherDto {
  id: string;
  voucherNo: string;
  voucherType: VoucherType;
  status: VoucherStatus;
  warehouseId: string;
  toWarehouseId: string | null;
  transferStatus: TransferStatus | null;
  createdBy: string | null;
  approvedBy: string | null;
  postedAt: string | null;
  lineCount: number;
}

/** سطر تفصيلي (VoucherLineDto). */
export interface VoucherLineDto {
  lineNo: number;
  itemId: string;
  itemCode: string;
  qty: number;
  qtyAccepted: number;
  qtyRejected: number;
  batchNo: string | null;
  serialNo: string | null;
  expiryDate: string | null;
  unitCost: number;
}

/** تفصيل الإذن (VoucherDetailDto). */
export interface VoucherDetailDto {
  id: string;
  voucherNo: string;
  voucherType: VoucherType;
  status: VoucherStatus;
  warehouseId: string;
  toWarehouseId: string | null;
  supplierId: string | null;
  transferStatus: TransferStatus | null;
  reason: string | null;
  createdBy: string | null;
  approvedBy: string | null;
  postedAt: string | null;
  documentDate: string | null;
  lines: VoucherLineDto[];
}

/** إدخال سطر عند الإنشاء (VoucherLineInput). */
export interface VoucherLineInput {
  itemId: string;
  locationId?: string | null;
  toLocationId?: string | null;
  qty: number;
  qtyAccepted?: number | null;
  qtyRejected?: number | null;
  rejectReason?: string | null;
  batchNo?: string | null;
  serialNo?: string | null;
  expiryDate?: string | null;
  unitCost?: number | null;
  notes?: string | null;
}

/** جسم إنشاء الإذن (CreateVoucherCommand). */
export interface CreateVoucherCommand {
  voucherType: VoucherType;
  warehouseId: string;
  toWarehouseId?: string | null;
  supplierId?: string | null;
  sourceVoucherId?: string | null;
  referenceNo?: string | null;
  documentDate?: string | null;
  requestingDept?: string | null;
  reason?: string | null;
  recipientEmployeeId?: string | null;
  adjustmentType?: AdjustmentType | null;
  notes?: string | null;
  lines: VoucherLineInput[];
}

export interface VouchersQuery {
  type?: VoucherType;
  status?: VoucherStatus;
  page: number;
  pageSize: number;
}

/** رصيد مخزون (StockBalanceDto). */
export interface StockBalanceDto {
  itemId: string;
  itemCode: string;
  itemName: string;
  warehouseId: string;
  warehouseCode: string;
  batchNo: string | null;
  serialNo: string | null;
  expiryDate: string | null;
  qtyOnHand: number;
  qtyReserved: number;
  qtyAvailable: number;
  avgCost: number;
}

/** مخزن (WarehouseDto). */
export interface WarehouseDto {
  id: string;
  code: string;
  nameAr: string;
  warehouseType: number;
  status: number;
  region: string | null;
  usesLocations: boolean;
  isActive: boolean;
}

/** مورّد (SupplierDto). */
export interface SupplierDto {
  id: string;
  code: string;
  nameAr: string;
  taxNumber: string | null;
  phone: string | null;
  isActive: boolean;
}

/** الإجراءات المتاحة على إذن حسب حالته. */
export function voucherActions(v: {
  status: VoucherStatus;
  voucherType: VoucherType;
  transferStatus: TransferStatus | null;
}): {
  canSubmit: boolean;
  canApprove: boolean;
  canReject: boolean;
  canCancel: boolean;
  canConfirmTransfer: boolean;
} {
  const isDraft = v.status === VoucherStatus.Draft;
  const isReview = v.status === VoucherStatus.UnderReview;
  return {
    canSubmit: isDraft,
    canApprove: isReview,
    canReject: isReview,
    canCancel: isDraft || isReview,
    canConfirmTransfer:
      v.voucherType === VoucherType.Transfer &&
      v.transferStatus === TransferStatus.InTransit,
  };
}
