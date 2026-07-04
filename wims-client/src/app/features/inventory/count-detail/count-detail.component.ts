import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { StockCountService } from '../stock-count.service';
import {
  CountEntry,
  STOCK_COUNT_STATUS_BADGE,
  STOCK_COUNT_STATUS_LABEL,
  STOCK_COUNT_TYPE_LABEL,
  StockCountDetailDto,
  StockCountLineDto,
  StockCountStatus,
  stockCountActions,
} from '../../../core/models/inventory.models';
import { ConfirmDialogComponent } from '../../../shared/confirm-dialog/confirm-dialog.component';

/**
 * محضر الجرد / كشف العدّ (المرحلة ٤).
 * يعرض البنود، ويتيح إدخال العدّ الفعلي عند التجميد، ثم رفع/اعتماد/إلغاء
 * حسب حالة المحضر. الفروقات تُحسب حيّاً، والاعتماد يرحّل تسويات تلقائياً.
 */
@Component({
  selector: 'wims-count-detail',
  standalone: true,
  imports: [DatePipe, DecimalPipe, FormsModule, RouterLink, ConfirmDialogComponent],
  templateUrl: './count-detail.component.html',
  styleUrl: './count-detail.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CountDetailComponent implements OnInit {
  private readonly service = inject(StockCountService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly detail = signal<StockCountDetailDto | null>(null);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly busy = signal(false);
  readonly actionError = signal<string | null>(null);
  readonly savedOk = signal(false);

  /** مسودّات العدّ الفعلي المُدخَلة محلياً: lineId → قيمة. */
  readonly edits = signal<Record<string, number>>({});

  readonly typeLabel = STOCK_COUNT_TYPE_LABEL;
  readonly statusLabel = STOCK_COUNT_STATUS_LABEL;
  readonly statusBadge = STOCK_COUNT_STATUS_BADGE;
  readonly StockCountStatus = StockCountStatus;

  private readonly cancelDlg =
    viewChild.required<ConfirmDialogComponent>('cancelDlg');
  private readonly approveDlg =
    viewChild.required<ConfirmDialogComponent>('approveDlg');

  readonly header = computed(() => this.detail()?.header ?? null);

  readonly actions = computed(() => {
    const h = this.header();
    return h
      ? stockCountActions(h.status)
      : { canFreeze: false, canCount: false, canSubmit: false, canApprove: false, canCancel: false };
  });

  /** أدوات المحضر أثناء التجميد: إدخال العدّ متاح. */
  readonly editable = computed(() => this.actions().canCount);

  /** قيمة الفرق الحيّة لبند = (المُدخَل ?? الفعلي ?? الدفتري) − الدفتري. */
  liveVariance(lineId: string, bookQty: number, physicalQty: number | null): number {
    const e = this.edits()[lineId];
    const phys = e !== undefined ? e : physicalQty ?? bookQty;
    return phys - bookQty;
  }

  /** قيمة خانة العدّ للعرض في حقل الإدخال (المُدخَل، وإلا الفعلي، وإلا فارغ). */
  physValue(l: StockCountLineDto): number | null {
    const e = this.edits()[l.id];
    return e !== undefined ? e : l.physicalQty;
  }

  private id = '';

  ngOnInit(): void {
    this.id = this.route.snapshot.paramMap.get('id') ?? '';
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.error.set(null);
    this.service.get(this.id).subscribe({
      next: (d) => {
        this.detail.set(d);
        // تهيئة المسودّات من القيم الحالية.
        const seed: Record<string, number> = {};
        for (const l of d.lines) {
          if (l.physicalQty !== null) seed[l.id] = l.physicalQty;
        }
        this.edits.set(seed);
        this.loading.set(false);
      },
      error: (err: { status?: number }) => {
        this.error.set(
          err?.status === 404 ? 'المحضر غير موجود.' : 'تعذّر تحميل المحضر.',
        );
        this.loading.set(false);
      },
    });
  }

  setEdit(lineId: string, value: string): void {
    const n = Number(value);
    this.edits.update((m) => ({ ...m, [lineId]: isNaN(n) ? 0 : Math.max(0, n) }));
  }

  saveCount(): void {
    const d = this.detail();
    if (!d) return;
    const entries: CountEntry[] = d.lines
      .filter((l) => this.edits()[l.id] !== undefined)
      .map((l) => ({ lineId: l.id, physicalQty: this.edits()[l.id] }));
    if (entries.length === 0) {
      this.actionError.set('أدخل قيمة عدّ واحدة على الأقل.');
      return;
    }
    this.actionError.set(null);
    this.savedOk.set(false);
    this.busy.set(true);
    this.service.enterCount(this.id, entries).subscribe({
      next: () => {
        this.busy.set(false);
        this.savedOk.set(true);
        this.reload();
      },
      error: (err) => this.fail(err),
    });
  }

  freeze(): void {
    this.run(this.service.freeze(this.id));
  }

  submit(): void {
    this.run(this.service.submit(this.id));
  }

  openApprove(): void {
    this.approveDlg().open();
  }
  onApprove(): void {
    this.run(this.service.approve(this.id));
  }

  openCancel(): void {
    this.cancelDlg().open();
  }
  onCancel(): void {
    this.run(this.service.cancel(this.id));
  }

  private run(obs: import('rxjs').Observable<void>): void {
    this.actionError.set(null);
    this.savedOk.set(false);
    this.busy.set(true);
    obs.subscribe({
      next: () => {
        this.busy.set(false);
        this.reload();
      },
      error: (err) => this.fail(err),
    });
  }

  private fail(err: { error?: { detail?: string; message?: string }; status?: number }): void {
    this.busy.set(false);
    this.actionError.set(
      err?.error?.detail ??
        err?.error?.message ??
        (err?.status === 403
          ? 'لا يمكنك اعتماد محضر جمّدته أو أنشأته (فصل المهام).'
          : 'تعذّر تنفيذ الإجراء. أعِد المحاولة.'),
    );
  }

  voucherNos(csv: string | null): string[] {
    return (csv ?? '')
      .split(',')
      .map((s) => s.trim())
      .filter(Boolean);
  }

  back(): void {
    this.router.navigateByUrl('/inventory/counts');
  }
}
