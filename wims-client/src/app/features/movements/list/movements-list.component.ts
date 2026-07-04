import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { VouchersService } from '../vouchers.service';
import {
  VOUCHER_STATUS_BADGE,
  VOUCHER_STATUS_LABEL,
  VOUCHER_TYPE_LABEL,
  VoucherDto,
  VoucherStatus,
  VoucherType,
} from '../../../core/models/voucher.models';
import { PaginatorComponent } from '../../../shared/paginator/paginator.component';
import { EmptyStateComponent } from '../../../shared/empty-state/empty-state.component';
import { HasPermissionDirective } from '../../../shared/has-permission.directive';

@Component({
  selector: 'wims-movements-list',
  standalone: true,
  imports: [
    FormsModule,
    DatePipe,
    RouterLink,
    PaginatorComponent,
    EmptyStateComponent,
    HasPermissionDirective,
  ],
  templateUrl: './movements-list.component.html',
  styleUrl: './movements-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MovementsListComponent implements OnInit {
  private readonly service = inject(VouchersService);

  readonly rows = signal<VoucherDto[]>([]);
  readonly total = signal(0);
  readonly page = signal(1);
  readonly pageSize = 20;
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  private readonly warehouseNames = signal<Map<string, string>>(new Map());

  type: VoucherType | '' = '';
  status: VoucherStatus | '' = '';

  readonly typeLabel = VOUCHER_TYPE_LABEL;
  readonly statusLabel = VOUCHER_STATUS_LABEL;
  readonly statusBadge = VOUCHER_STATUS_BADGE;

  readonly typeOptions = Object.entries(VOUCHER_TYPE_LABEL).map(([v, label]) => ({
    value: Number(v) as VoucherType,
    label,
  }));
  readonly statusOptions = Object.entries(VOUCHER_STATUS_LABEL).map(
    ([v, label]) => ({ value: Number(v) as VoucherStatus, label }),
  );

  readonly skeletonRows = Array.from({ length: 8 });

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
        type: this.type || undefined,
        status: this.status || undefined,
        page: this.page(),
        pageSize: this.pageSize,
      })
      .subscribe({
        next: (res) => {
          this.rows.set(res.items);
          this.total.set(res.totalCount);
          this.loading.set(false);
        },
        error: () => {
          this.error.set('تعذّر تحميل الأذون. تأكّد من اتصال الخادم ثم أعِد المحاولة.');
          this.loading.set(false);
        },
      });
  }

  warehouseName(id: string | null): string {
    if (!id) return '—';
    return this.warehouseNames().get(id) ?? '—';
  }

  onFilterChange(): void {
    this.page.set(1);
    this.load();
  }

  onPageChange(p: number): void {
    this.page.set(p);
    this.load();
  }
}
