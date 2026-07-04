import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AlertsService } from '../alerts.service';
import { VouchersService } from '../../movements/vouchers.service';
import {
  ALERT_SEVERITY_BADGE,
  ALERT_SEVERITY_LABEL,
  ALERT_STATUS_BADGE,
  ALERT_STATUS_LABEL,
  ALERT_TYPE_LABEL,
  AlertDto,
  AlertScanSummary,
  AlertStatus,
  AlertType,
} from '../../../core/models/inventory.models';
import { EmptyStateComponent } from '../../../shared/empty-state/empty-state.component';

/**
 * التنبيهات (المرحلة ٤) — حدّ أدنى/إعادة طلب/صلاحية/ركود.
 * القائمة مرتّبة بالخطورة على الخادم؛ إجراءات: اطّلاع/معالجة + فحص يدوي.
 */
@Component({
  selector: 'wims-alerts',
  standalone: true,
  imports: [DatePipe, FormsModule, RouterLink, EmptyStateComponent],
  templateUrl: './alerts.component.html',
  styleUrl: './alerts.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AlertsComponent implements OnInit {
  private readonly service = inject(AlertsService);
  private readonly vouchers = inject(VouchersService);

  readonly rows = signal<AlertDto[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly busyId = signal<string | null>(null);
  readonly actionError = signal<string | null>(null);
  readonly scanning = signal(false);
  readonly scanSummary = signal<AlertScanSummary | null>(null);

  private readonly warehouseNames = signal<Map<string, string>>(new Map());

  status: AlertStatus | '' = '';
  type: AlertType | '' = '';

  readonly typeLabel = ALERT_TYPE_LABEL;
  readonly sevLabel = ALERT_SEVERITY_LABEL;
  readonly sevBadge = ALERT_SEVERITY_BADGE;
  readonly statusLabel = ALERT_STATUS_LABEL;
  readonly statusBadge = ALERT_STATUS_BADGE;

  readonly typeOptions = Object.entries(ALERT_TYPE_LABEL).map(([v, label]) => ({
    value: Number(v) as AlertType,
    label,
  }));
  readonly statusOptions = Object.entries(ALERT_STATUS_LABEL).map(
    ([v, label]) => ({ value: Number(v) as AlertStatus, label }),
  );

  readonly skeletonRows = Array.from({ length: 6 });
  readonly AlertStatus = AlertStatus;

  ngOnInit(): void {
    this.vouchers.warehouses().subscribe({
      next: (ws) =>
        this.warehouseNames.set(new Map(ws.map((w) => [w.id, w.nameAr]))),
      error: () => {
        /* تحسين فقط */
      },
    });
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.service
      .list(this.status || undefined, this.type || undefined)
      .subscribe({
        next: (list) => {
          this.rows.set(list);
          this.loading.set(false);
        },
        error: () => {
          this.error.set('تعذّر تحميل التنبيهات. تأكّد من اتصال الخادم ثم أعِد المحاولة.');
          this.loading.set(false);
        },
      });
  }

  onFilterChange(): void {
    this.load();
  }

  warehouseName(id: string | null): string {
    if (!id) return 'كل المخازن';
    return this.warehouseNames().get(id) ?? '—';
  }

  acknowledge(a: AlertDto): void {
    this.act(a.id, this.service.acknowledge(a.id));
  }

  resolve(a: AlertDto): void {
    this.act(a.id, this.service.resolve(a.id));
  }

  private act(id: string, obs: import('rxjs').Observable<void>): void {
    this.busyId.set(id);
    this.actionError.set(null);
    obs.subscribe({
      next: () => {
        this.busyId.set(null);
        this.load();
      },
      error: (err: { error?: { detail?: string; message?: string }; status?: number }) => {
        this.busyId.set(null);
        this.actionError.set(
          err?.error?.detail ??
            err?.error?.message ??
            (err?.status === 403 ? 'لا تملك صلاحية هذا الإجراء.' : 'تعذّر تنفيذ الإجراء.'),
        );
      },
    });
  }

  runScan(): void {
    this.scanning.set(true);
    this.actionError.set(null);
    this.scanSummary.set(null);
    this.service.scan().subscribe({
      next: (summary) => {
        this.scanning.set(false);
        this.scanSummary.set(summary);
        this.load();
      },
      error: (err: { error?: { detail?: string; message?: string } }) => {
        this.scanning.set(false);
        this.actionError.set(
          err?.error?.detail ?? err?.error?.message ?? 'تعذّر تشغيل الفحص.',
        );
      },
    });
  }
}
