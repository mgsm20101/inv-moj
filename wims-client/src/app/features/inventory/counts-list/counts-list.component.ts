import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { StockCountService } from '../stock-count.service';
import {
  STOCK_COUNT_STATUS_BADGE,
  STOCK_COUNT_STATUS_LABEL,
  STOCK_COUNT_TYPE_LABEL,
  StockCountDto,
  StockCountStatus,
} from '../../../core/models/inventory.models';
import { EmptyStateComponent } from '../../../shared/empty-state/empty-state.component';

/** قائمة محاضر الجرد (المرحلة ٤). قائمة خام مرتّبة بالأحدث؛ فلتر بالحالة. */
@Component({
  selector: 'wims-counts-list',
  standalone: true,
  imports: [DatePipe, DecimalPipe, FormsModule, RouterLink, EmptyStateComponent],
  templateUrl: './counts-list.component.html',
  styleUrl: './counts-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CountsListComponent implements OnInit {
  private readonly service = inject(StockCountService);

  readonly rows = signal<StockCountDto[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  status: StockCountStatus | '' = '';

  readonly typeLabel = STOCK_COUNT_TYPE_LABEL;
  readonly statusLabel = STOCK_COUNT_STATUS_LABEL;
  readonly statusBadge = STOCK_COUNT_STATUS_BADGE;

  readonly statusOptions = Object.entries(STOCK_COUNT_STATUS_LABEL).map(
    ([v, label]) => ({ value: Number(v) as StockCountStatus, label }),
  );

  readonly skeletonRows = Array.from({ length: 6 });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.service.list(this.status || undefined).subscribe({
      next: (list) => {
        this.rows.set(list);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('تعذّر تحميل محاضر الجرد. تأكّد من اتصال الخادم ثم أعِد المحاولة.');
        this.loading.set(false);
      },
    });
  }

  onFilterChange(): void {
    this.load();
  }
}
