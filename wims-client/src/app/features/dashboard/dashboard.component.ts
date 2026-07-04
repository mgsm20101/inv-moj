import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ReportsService } from '../reports/reports.service';
import { DashboardDto } from '../../core/models/reports.models';
import {
  ALERT_SEVERITY_BADGE,
  ALERT_SEVERITY_LABEL,
  ALERT_TYPE_LABEL,
  AlertSeverity,
  AlertType,
} from '../../core/models/inventory.models';

interface Kpi {
  label: string;
  value: number;
  hint: string;
  tone: 'neutral' | 'warning' | 'critical' | 'justice';
  link?: string;
}

/** الرئيسية — مؤشرات تشغيلية حيّة من /api/dashboard. */
@Component({
  selector: 'wims-dashboard',
  standalone: true,
  imports: [DatePipe, DecimalPipe, RouterLink],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardComponent implements OnInit {
  private readonly service = inject(ReportsService);

  readonly data = signal<DashboardDto | null>(null);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  readonly sevLabel = ALERT_SEVERITY_LABEL;
  readonly sevBadge = ALERT_SEVERITY_BADGE;
  readonly typeLabel = ALERT_TYPE_LABEL;

  readonly skeletonTiles = Array.from({ length: 8 });

  /** بطاقات المؤشرات مشتقّة من البيانات الحيّة. */
  readonly kpis = computed<Kpi[]>(() => {
    const d = this.data();
    if (!d) return [];
    return [
      { label: 'أذون بانتظار الاعتماد', value: d.pendingVouchers, hint: 'تحتاج مراجعتك', tone: d.pendingVouchers > 0 ? 'warning' : 'neutral', link: '/approvals' },
      { label: 'تنبيهات مفتوحة', value: d.openAlerts, hint: `${d.criticalAlerts} حرِج · ${d.warningAlerts} تحذير`, tone: d.criticalAlerts > 0 ? 'critical' : d.openAlerts > 0 ? 'warning' : 'neutral', link: '/inventory' },
      { label: 'أصناف تحت الحد الأدنى', value: d.belowMinCount, hint: 'يلزم إعادة طلب', tone: d.belowMinCount > 0 ? 'warning' : 'neutral', link: '/reports' },
      { label: 'محاضر جرد مفتوحة', value: d.openStockCounts, hint: 'قيد التجميد/المراجعة', tone: d.openStockCounts > 0 ? 'warning' : 'neutral', link: '/inventory/counts' },
      { label: 'دفعات قاربت الصلاحية', value: d.nearExpiryBatches, hint: 'خلال ٣٠ يوماً', tone: d.nearExpiryBatches > 0 ? 'warning' : 'neutral', link: '/inventory' },
      { label: 'دفعات منتهية الصلاحية', value: d.expiredBatches, hint: 'يلزم إجراء', tone: d.expiredBatches > 0 ? 'critical' : 'neutral', link: '/inventory' },
      { label: 'أصناف في المخزون', value: d.itemsInStock, hint: `من ${d.activeItems} صنف نشط`, tone: 'justice' },
      { label: 'المخازن', value: d.totalWarehouses, hint: 'مخزن نشط', tone: 'neutral' },
    ];
  });

  readonly stockValue = computed(() => this.data()?.totalStockValue ?? 0);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.service.dashboard().subscribe({
      next: (d) => {
        this.data.set(d);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('تعذّر تحميل لوحة المعلومات. تأكّد من اتصال الخادم ثم أعِد المحاولة.');
        this.loading.set(false);
      },
    });
  }

  readonly AlertType = AlertType;
  readonly AlertSeverity = AlertSeverity;
}
