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
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { VouchersService } from '../vouchers.service';
import {
  ADJUSTMENT_TYPE_LABEL,
  TRANSFER_STATUS_LABEL,
  VOUCHER_STATUS_BADGE,
  VOUCHER_STATUS_LABEL,
  VOUCHER_TYPE_LABEL,
  VoucherDetailDto,
  VoucherStatus,
  voucherActions,
} from '../../../core/models/voucher.models';
import { ConfirmDialogComponent } from '../../../shared/confirm-dialog/confirm-dialog.component';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'wims-voucher-detail',
  standalone: true,
  imports: [
    DatePipe,
    DecimalPipe,
    RouterLink,
    ConfirmDialogComponent,
  ],
  templateUrl: './voucher-detail.component.html',
  styleUrl: './voucher-detail.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VoucherDetailComponent implements OnInit {
  private readonly service = inject(VouchersService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);

  readonly voucher = signal<VoucherDetailDto | null>(null);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly busy = signal(false);
  readonly actionError = signal<string | null>(null);
  /** يُشغّل حركة دمغة الختم بعد اعتماد ناجح. */
  readonly stamped = signal(false);

  readonly typeLabel = VOUCHER_TYPE_LABEL;
  readonly statusLabel = VOUCHER_STATUS_LABEL;
  readonly statusBadge = VOUCHER_STATUS_BADGE;
  readonly transferLabel = TRANSFER_STATUS_LABEL;
  readonly adjustmentLabel = ADJUSTMENT_TYPE_LABEL;

  private readonly rejectDlg =
    viewChild.required<ConfirmDialogComponent>('rejectDlg');
  private readonly cancelDlg =
    viewChild.required<ConfirmDialogComponent>('cancelDlg');

  readonly actions = computed(() => {
    const v = this.voucher();
    return v
      ? voucherActions(v)
      : {
          canSubmit: false,
          canApprove: false,
          canReject: false,
          canCancel: false,
          canConfirmTransfer: false,
        };
  });

  /**
   * زرّا الاعتماد/الرفض يعتمدان على صلاحية Vouchers.Approve بالإضافة لحالة المستند.
   * محسوبة هنا مباشرةً (لا عبر *appHasPermission) لأن تكرار التوجيه الهيكلي على
   * زرّين متجاورين بنفس مفتاح الصلاحية أظهر سلوكاً غير موثوق (أحدهما لا يُنشئ
   * عنصره رغم أن الشرط والصلاحية صحيحان لكليهما — تحقّق مباشر عبر فحص الـ DOM).
   */
  private readonly canActOnApproval = computed(() =>
    this.auth.hasPermission('Vouchers.Approve'),
  );
  readonly showApprove = computed(
    () => this.actions().canApprove && this.canActOnApproval(),
  );
  readonly showReject = computed(
    () => this.actions().canReject && this.canActOnApproval(),
  );

  readonly totalCost = computed(() =>
    (this.voucher()?.lines ?? []).reduce((s, l) => s + l.qty * l.unitCost, 0),
  );

  private id = '';

  ngOnInit(): void {
    this.id = this.route.snapshot.paramMap.get('id') ?? '';
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.error.set(null);
    this.service.get(this.id).subscribe({
      next: (v) => {
        this.voucher.set(v);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(
          err?.status === 404
            ? 'المستند غير موجود.'
            : 'تعذّر تحميل المستند.',
        );
        this.loading.set(false);
      },
    });
  }

  submit(): void {
    this.run(this.service.submit(this.id));
  }

  approve(): void {
    this.actionError.set(null);
    this.busy.set(true);
    this.service.approve(this.id).subscribe({
      next: () => {
        this.busy.set(false);
        this.stamped.set(true); // شغّل دمغة الختم
        this.service.get(this.id).subscribe((v) => this.voucher.set(v));
      },
      error: (err) => this.fail(err),
    });
  }

  onReject(reason: string | undefined): void {
    if (!reason) {
      this.actionError.set('سبب الرفض مطلوب.');
      return;
    }
    this.run(this.service.reject(this.id, reason));
  }

  onCancel(): void {
    this.run(this.service.cancel(this.id));
  }

  confirmTransfer(): void {
    this.run(this.service.confirmTransfer(this.id));
  }

  openReject(): void {
    this.rejectDlg().open();
  }
  openCancel(): void {
    this.cancelDlg().open();
  }

  private run(obs: import('rxjs').Observable<void>): void {
    this.actionError.set(null);
    this.busy.set(true);
    obs.subscribe({
      next: () => {
        this.busy.set(false);
        this.reload();
      },
      error: (err) => this.fail(err),
    });
  }

  private fail(err: { error?: { message?: string }; status?: number }): void {
    this.busy.set(false);
    this.actionError.set(
      err?.error?.message ??
        (err?.status === 403
          ? 'لا تملك صلاحية هذا الإجراء.'
          : 'تعذّر تنفيذ الإجراء. أعِد المحاولة.'),
    );
  }

  back(): void {
    this.router.navigateByUrl('/movements');
  }

  readonly VoucherStatus = VoucherStatus;
}
