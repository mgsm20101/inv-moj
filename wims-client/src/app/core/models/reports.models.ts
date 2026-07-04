// نماذج المرحلة ٥: التقارير (Reports) ولوحة المعلومات (Dashboard).
// المصادر: WIMS.WebApi/Controllers/{ReportsController,DashboardController}.cs،
//          WIMS.Application/Features/{Reports,Dashboard}/*.
// ملاحظة: كل التقارير الستة ترجع نفس النوع العام ReportDocument.

import { AlertSeverity, AlertType } from './inventory.models';

// ═══════════════════════ مستند تقرير عام ═══════════════════════

/** محاذاة عمود التقرير — ReportAlignment (int). */
export enum ReportAlignment {
  Right = 0,
  Center = 1,
  Left = 2,
}

export interface ReportColumn {
  header: string;
  align: ReportAlignment;
  width: number;
}

export interface ReportMeta {
  label: string;
  value: string;
}

/**
 * مستند تقرير جاهز للعرض. الخلايا نصوص مُنسّقة مسبقاً على الخادم
 * (لا تنسيق في الواجهة). rows: مصفوفة صفوف، كل صف مصفوفة خلايا نصية.
 */
export interface ReportDocument {
  title: string;
  subtitle: string | null;
  meta: ReportMeta[];
  columns: ReportColumn[];
  rows: string[][];
  totals: string[] | null;
  generatedAt: string;
}

/** صيغة تصدير التقرير عبر ?format=. */
export type ReportExportFormat = 'pdf' | 'excel';

// ═══════════════════════ لوحة المعلومات (Dashboard) ═══════════════════════

export interface DashboardShortageDto {
  itemCode: string;
  itemName: string;
  onHand: number;
  minStock: number;
  shortage: number;
}

export interface DashboardAlertDto {
  id: string;
  alertType: AlertType;
  severity: AlertSeverity;
  itemName: string;
  message: string;
  detectedAt: string;
}

export interface DashboardDto {
  totalItems: number;
  activeItems: number;
  totalWarehouses: number;
  totalStockValue: number;
  itemsInStock: number;
  belowMinCount: number;
  nearExpiryBatches: number;
  expiredBatches: number;
  pendingVouchers: number;
  openStockCounts: number;
  openAlerts: number;
  criticalAlerts: number;
  warningAlerts: number;
  topShortages: DashboardShortageDto[];
  recentCriticalAlerts: DashboardAlertDto[];
}
