// نماذج المرحلة ٤: الجرد (StockCount) والتنبيهات (Alerts) — مطابقة لعقود الـ backend.
// المصادر: WIMS.WebApi/Controllers/{StockCountController,AlertsController}.cs،
//          WIMS.Application/Features/{StockCount,Alerts}/*، والـ Enums.

// ═══════════════════════ الجرد (Stock Count) ═══════════════════════

/** نوع محضر الجرد — StockCountType (byte). */
export enum StockCountType {
  Full = 1,
  Partial = 2,
  Cyclic = 3,
}

export const STOCK_COUNT_TYPE_LABEL: Record<StockCountType, string> = {
  [StockCountType.Full]: 'جرد شامل',
  [StockCountType.Partial]: 'جرد جزئي',
  [StockCountType.Cyclic]: 'جرد دوري',
};

/** حالة محضر الجرد — StockCountStatus (byte) — آلة حالات. */
export enum StockCountStatus {
  Draft = 1,
  Frozen = 2,
  UnderReview = 3,
  Approved = 4,
  Cancelled = 5,
}

export const STOCK_COUNT_STATUS_LABEL: Record<StockCountStatus, string> = {
  [StockCountStatus.Draft]: 'مسودة',
  [StockCountStatus.Frozen]: 'مُجمَّد',
  [StockCountStatus.UnderReview]: 'قيد المراجعة',
  [StockCountStatus.Approved]: 'معتمَد',
  [StockCountStatus.Cancelled]: 'مُلغى',
};

export const STOCK_COUNT_STATUS_BADGE: Record<StockCountStatus, string> = {
  [StockCountStatus.Draft]: 'badge--draft',
  [StockCountStatus.Frozen]: 'badge--info',
  [StockCountStatus.UnderReview]: 'badge--pending',
  [StockCountStatus.Approved]: 'badge--approved',
  [StockCountStatus.Cancelled]: 'badge--rejected',
};

export interface StockCountDto {
  id: string;
  countNo: string;
  countType: StockCountType;
  status: StockCountStatus;
  warehouseId: string;
  warehouseName: string;
  scopeNote: string | null;
  frozenAt: string | null;
  frozenBy: string | null;
  countedAt: string | null;
  approvedBy: string | null;
  approvedAt: string | null;
  adjustmentVoucherNos: string | null;
  lineCount: number;
  totalVarianceValue: number;
}

export interface StockCountLineDto {
  id: string;
  lineNo: number;
  itemId: string;
  itemCode: string;
  itemName: string;
  locationId: string | null;
  batchNo: string | null;
  serialNo: string | null;
  expiryDate: string | null;
  bookQty: number;
  physicalQty: number | null;
  varianceQty: number;
  unitCost: number;
  varianceValue: number;
  counted: boolean;
}

export interface StockCountDetailDto {
  header: StockCountDto;
  lines: StockCountLineDto[];
}

/** أمر إنشاء محضر جرد — PlanStockCountCommand. */
export interface PlanStockCountCommand {
  warehouseId: string;
  countType: StockCountType;
  itemIds: string[];
  scopeNote?: string | null;
}

/** إدخال عدّ فعلي لبند — CountEntry. */
export interface CountEntry {
  lineId: string;
  physicalQty: number;
}

/** الإجراءات المتاحة على محضر الجرد حسب حالته. */
export function stockCountActions(status: StockCountStatus): {
  canFreeze: boolean;
  canCount: boolean;
  canSubmit: boolean;
  canApprove: boolean;
  canCancel: boolean;
} {
  return {
    canFreeze: status === StockCountStatus.Draft,
    canCount: status === StockCountStatus.Frozen,
    canSubmit: status === StockCountStatus.Frozen,
    canApprove: status === StockCountStatus.UnderReview,
    canCancel:
      status === StockCountStatus.Draft ||
      status === StockCountStatus.Frozen ||
      status === StockCountStatus.UnderReview,
  };
}

// ═══════════════════════ التنبيهات (Alerts) ═══════════════════════

/** نوع التنبيه — AlertType (byte). */
export enum AlertType {
  ReorderPoint = 1,
  MinStock = 2,
  NearExpiry = 3,
  Expired = 4,
  Stagnant = 5,
}

export const ALERT_TYPE_LABEL: Record<AlertType, string> = {
  [AlertType.ReorderPoint]: 'نقطة إعادة الطلب',
  [AlertType.MinStock]: 'حد أدنى للمخزون',
  [AlertType.NearExpiry]: 'قارب انتهاء الصلاحية',
  [AlertType.Expired]: 'منتهي الصلاحية',
  [AlertType.Stagnant]: 'صنف راكد',
};

/** خطورة التنبيه — AlertSeverity (byte). */
export enum AlertSeverity {
  Info = 1,
  Warning = 2,
  Critical = 3,
}

export const ALERT_SEVERITY_LABEL: Record<AlertSeverity, string> = {
  [AlertSeverity.Info]: 'معلومة',
  [AlertSeverity.Warning]: 'تحذير',
  [AlertSeverity.Critical]: 'حرج',
};

export const ALERT_SEVERITY_BADGE: Record<AlertSeverity, string> = {
  [AlertSeverity.Info]: 'badge--info',
  [AlertSeverity.Warning]: 'badge--pending',
  [AlertSeverity.Critical]: 'badge--rejected',
};

/** حالة التنبيه — AlertStatus (byte). */
export enum AlertStatus {
  Open = 1,
  Acknowledged = 2,
  Resolved = 3,
}

export const ALERT_STATUS_LABEL: Record<AlertStatus, string> = {
  [AlertStatus.Open]: 'مفتوح',
  [AlertStatus.Acknowledged]: 'مُطَّلع عليه',
  [AlertStatus.Resolved]: 'مُعالَج',
};

export const ALERT_STATUS_BADGE: Record<AlertStatus, string> = {
  [AlertStatus.Open]: 'badge--pending',
  [AlertStatus.Acknowledged]: 'badge--info',
  [AlertStatus.Resolved]: 'badge--approved',
};

export interface AlertDto {
  id: string;
  alertType: AlertType;
  severity: AlertSeverity;
  status: AlertStatus;
  itemId: string;
  itemCode: string;
  itemName: string;
  warehouseId: string | null;
  batchNo: string | null;
  message: string;
  observedValue: number | null;
  thresholdValue: number | null;
  detectedAt: string;
  acknowledgedBy: string | null;
  acknowledgedAt: string | null;
  resolvedAt: string | null;
}

/** ملخّص فحص التنبيهات — AlertScanSummary. */
export interface AlertScanSummary {
  created: number;
  resolved: number;
  criticalNew: number;
}
