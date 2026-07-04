import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import {
  FormControl,
  FormsModule,
  ReactiveFormsModule,
} from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { StockCountService } from '../stock-count.service';
import { VouchersService } from '../../movements/vouchers.service';
import { ItemsService } from '../../items/items.service';
import { WarehouseDto } from '../../../core/models/voucher.models';
import { ItemDto } from '../../../core/models/catalog.models';
import {
  STOCK_COUNT_TYPE_LABEL,
  StockCountType,
} from '../../../core/models/inventory.models';

/** إنشاء محضر جرد (مسودة). الجرد الجزئي يتطلب اختيار أصناف. */
@Component({
  selector: 'wims-count-plan',
  standalone: true,
  imports: [FormsModule, ReactiveFormsModule, RouterLink],
  templateUrl: './count-plan.component.html',
  styleUrl: './count-plan.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CountPlanComponent implements OnInit {
  private readonly service = inject(StockCountService);
  private readonly vouchers = inject(VouchersService);
  private readonly items = inject(ItemsService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly warehouses = signal<WarehouseDto[]>([]);
  readonly StockCountType = StockCountType;
  readonly typeLabel = STOCK_COUNT_TYPE_LABEL;
  readonly typeOptions = Object.entries(STOCK_COUNT_TYPE_LABEL).map(
    ([v, label]) => ({ value: Number(v) as StockCountType, label }),
  );

  warehouseId = '';
  countType: StockCountType = StockCountType.Full;
  scopeNote = '';

  // منتقي الأصناف (للجرد الجزئي)
  readonly itemSearch = new FormControl('', { nonNullable: true });
  readonly itemResults = signal<ItemDto[]>([]);
  readonly searching = signal(false);
  readonly selected = signal<ItemDto[]>([]);
  readonly selectedIds = computed(
    () => new Set(this.selected().map((i) => i.id)),
  );

  readonly saving = signal(false);
  readonly serverError = signal<string | null>(null);
  readonly submitted = signal(false);

  readonly isPartial = computed(() => this.countType === StockCountType.Partial);

  ngOnInit(): void {
    this.vouchers.warehouses().subscribe({
      next: (ws) => this.warehouses.set(ws.filter((w) => w.isActive)),
      error: () => this.warehouses.set([]),
    });

    this.itemSearch.valueChanges
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((term) => this.searchItems(term));
  }

  private searchItems(term: string): void {
    if (!term.trim()) {
      this.itemResults.set([]);
      return;
    }
    this.searching.set(true);
    this.items.getItems({ search: term, page: 1, pageSize: 20 }).subscribe({
      next: (res) => {
        this.itemResults.set(res.items);
        this.searching.set(false);
      },
      error: () => {
        this.itemResults.set([]);
        this.searching.set(false);
      },
    });
  }

  toggleItem(item: ItemDto): void {
    this.selected.update((list) =>
      list.some((i) => i.id === item.id)
        ? list.filter((i) => i.id !== item.id)
        : [...list, item],
    );
  }

  removeItem(id: string): void {
    this.selected.update((list) => list.filter((i) => i.id !== id));
  }

  onTypeChange(): void {
    if (!this.isPartial()) {
      this.selected.set([]);
      this.itemResults.set([]);
      this.itemSearch.setValue('');
    }
  }

  submit(): void {
    this.submitted.set(true);
    this.serverError.set(null);
    if (!this.warehouseId) return;
    if (this.isPartial() && this.selected().length === 0) return;

    this.saving.set(true);
    this.service
      .plan({
        warehouseId: this.warehouseId,
        countType: this.countType,
        itemIds: this.isPartial() ? this.selected().map((i) => i.id) : [],
        scopeNote: this.scopeNote.trim() || null,
      })
      .subscribe({
        next: (id) => {
          this.saving.set(false);
          this.router.navigate(['/inventory/counts', id]);
        },
        error: (err: { error?: { detail?: string; message?: string }; status?: number }) => {
          this.saving.set(false);
          this.serverError.set(
            err?.error?.detail ??
              err?.error?.message ??
              (err?.status === 409
                ? 'يوجد محضر جرد نشط لهذا المخزن بالفعل.'
                : 'تعذّر إنشاء المحضر. أعِد المحاولة.'),
          );
        },
      });
  }
}
