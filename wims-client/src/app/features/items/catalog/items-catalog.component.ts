import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import { FormControl, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ItemsService } from '../items.service';
import {
  CategoryDto,
  ITEM_TYPE_LABEL,
  ItemDto,
  ItemType,
} from '../../../core/models/catalog.models';
import { PaginatorComponent } from '../../../shared/paginator/paginator.component';
import { EmptyStateComponent } from '../../../shared/empty-state/empty-state.component';
import { HasPermissionDirective } from '../../../shared/has-permission.directive';

type StatusFilter = 'all' | 'active' | 'inactive';

@Component({
  selector: 'wims-items-catalog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    FormsModule,
    DecimalPipe,
    RouterLink,
    PaginatorComponent,
    EmptyStateComponent,
    HasPermissionDirective,
  ],
  templateUrl: './items-catalog.component.html',
  styleUrl: './items-catalog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ItemsCatalogComponent implements OnInit {
  private readonly service = inject(ItemsService);
  private readonly destroyRef = inject(DestroyRef);

  readonly items = signal<ItemDto[]>([]);
  readonly total = signal(0);
  readonly page = signal(1);
  readonly pageSize = 20;
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  readonly categories = signal<CategoryDto[]>([]);
  readonly searchCtrl = new FormControl('', { nonNullable: true });
  categoryId: string | '' = '';
  status: StatusFilter = 'active';

  readonly typeLabel = ITEM_TYPE_LABEL;
  readonly ItemType = ItemType;

  // عناصر placeholder الهيكل العظمي (skeleton) أثناء التحميل
  readonly skeletonRows = Array.from({ length: 8 });

  ngOnInit(): void {
    this.service
      .getCategories()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (c) => this.categories.set(c.filter((x) => x.isLeaf)),
        error: () => {
          /* الفلتر اختياري؛ لا نعطّل الشاشة */
        },
      });

    this.searchCtrl.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.page.set(1);
        this.load();
      });

    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.service
      .getItems({
        search: this.searchCtrl.value.trim() || undefined,
        categoryId: this.categoryId || undefined,
        isActive:
          this.status === 'all' ? undefined : this.status === 'active',
        page: this.page(),
        pageSize: this.pageSize,
      })
      .subscribe({
        next: (res) => {
          this.items.set(res.items);
          this.total.set(res.totalCount);
          this.loading.set(false);
        },
        error: () => {
          this.error.set('تعذّر تحميل الأصناف. تأكّد من اتصال الخادم ثم أعِد المحاولة.');
          this.loading.set(false);
        },
      });
  }

  onFilterChange(): void {
    this.page.set(1);
    this.load();
  }

  onPageChange(p: number): void {
    this.page.set(p);
    this.load();
  }

  trackId(_: number, item: ItemDto): string {
    return item.id;
  }
}
