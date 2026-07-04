import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { VouchersService } from '../../movements/vouchers.service';
import {
  VOUCHER_TYPE_LABEL,
  VoucherDto,
  VoucherStatus,
} from '../../../core/models/voucher.models';
import { EmptyStateComponent } from '../../../shared/empty-state/empty-state.component';

/**
 * صندوق الموافقات (المرحلة ٣).
 * لا يوجد endpoint مخصّص للصندوق في الـ backend؛ البديل الرسمي هو
 * سرد الأذون بحالة «قيد المراجعة» (UnderReview) والانتقال لشاشة التفصيل
 * حيث تتم إجراءات الاعتماد/الرفض/الختم.
 */
@Component({
  selector: 'wims-approvals-inbox',
  standalone: true,
  imports: [DatePipe, RouterLink, EmptyStateComponent],
  templateUrl: './approvals-inbox.component.html',
  styleUrl: './approvals-inbox.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ApprovalsInboxComponent implements OnInit {
  private readonly service = inject(VouchersService);

  readonly rows = signal<VoucherDto[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  private readonly warehouseNames = signal<Map<string, string>>(new Map());

  readonly typeLabel = VOUCHER_TYPE_LABEL;
  readonly skeletonRows = Array.from({ length: 6 });

  /** عدد المستندات المنتظرة — يُعرض كنص هادئ لا كبطاقة رقم ضخم. */
  readonly pendingCount = computed(() => this.rows().length);

  ngOnInit(): void {
    this.service.warehouses().subscribe({
      next: (ws) =>
        this.warehouseNames.set(new Map(ws.map((w) => [w.id, w.nameAr]))),
      error: () => {
        /* الأسماء تحسين فقط */
      },
    });
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.service
      .list({
        status: VoucherStatus.UnderReview,
        page: 1,
        pageSize: 200,
      })
      .subscribe({
        next: (res) => {
          this.rows.set(res.items);
          this.loading.set(false);
        },
        error: () => {
          this.error.set(
            'تعذّر تحميل صندوق الموافقات. تأكّد من اتصال الخادم ثم أعِد المحاولة.',
          );
          this.loading.set(false);
        },
      });
  }

  warehouseName(id: string | null): string {
    if (!id) return '—';
    return this.warehouseNames().get(id) ?? '—';
  }
}
