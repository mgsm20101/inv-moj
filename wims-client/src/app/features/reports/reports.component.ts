import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { FormControl, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { ReportParams, ReportsService } from './reports.service';
import { ReportDocumentComponent } from './report-document/report-document.component';
import { VouchersService } from '../movements/vouchers.service';
import { CustodyService } from '../custody/custody.service';
import { ItemsService } from '../items/items.service';
import { StockCountService } from './../inventory/stock-count.service';
import { WarehouseDto } from '../../core/models/voucher.models';
import { EmployeeDto } from '../../core/models/custody.models';
import { ItemDto } from '../../core/models/catalog.models';
import {
  STOCK_COUNT_STATUS_LABEL,
  STOCK_COUNT_TYPE_LABEL,
  StockCountDto,
} from '../../core/models/inventory.models';
import {
  ReportDocument,
  ReportExportFormat,
} from '../../core/models/reports.models';

type ReportKey =
  | 'stock-balance'
  | 'below-min'
  | 'stagnant'
  | 'custody'
  | 'item-card'
  | 'stock-count';

interface ReportDef {
  key: ReportKey;
  title: string;
  desc: string;
  needsWarehouse: boolean;
  needsOnlyInStock: boolean;
  needsDays: boolean;
  needsEmployee: boolean; // اختياري
  needsItem: boolean; // إجباري
  needsDates: boolean;
  needsStockCount: boolean; // إجباري
}

/**
 * مركز التقارير (المرحلة ٥) — يقود التقارير الستة بفلاتر ديناميكية،
 * عرض المستند (json)، وتصدير PDF/Excel لنفس المسار.
 */
@Component({
  selector: 'wims-reports',
  standalone: true,
  imports: [FormsModule, ReactiveFormsModule, ReportDocumentComponent],
  templateUrl: './reports.component.html',
  styleUrl: './reports.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReportsComponent implements OnInit {
  private readonly service = inject(ReportsService);
  private readonly vouchers = inject(VouchersService);
  private readonly custody = inject(CustodyService);
  private readonly itemsSvc = inject(ItemsService);
  private readonly counts = inject(StockCountService);
  private readonly destroyRef = inject(DestroyRef);

  readonly reports: ReportDef[] = [
    { key: 'stock-balance', title: 'رصيد المخزون', desc: 'أرصدة الأصناف وقيمتها حسب المخزن.', needsWarehouse: true, needsOnlyInStock: true, needsDays: false, needsEmployee: false, needsItem: false, needsDates: false, needsStockCount: false },
    { key: 'below-min', title: 'تحت الحد الأدنى', desc: 'أصناف بلغت أو تجاوزت حدّها الأدنى.', needsWarehouse: true, needsOnlyInStock: false, needsDays: false, needsEmployee: false, needsItem: false, needsDates: false, needsStockCount: false },
    { key: 'stagnant', title: 'الأصناف الراكدة', desc: 'أصناف بلا حركة صرف خلال مدة.', needsWarehouse: true, needsOnlyInStock: false, needsDays: true, needsEmployee: false, needsItem: false, needsDates: false, needsStockCount: false },
    { key: 'custody', title: 'العُهد الشخصية', desc: 'بنود العُهد لدى الموظفين.', needsWarehouse: false, needsOnlyInStock: false, needsDays: false, needsEmployee: true, needsItem: false, needsDates: false, needsStockCount: false },
    { key: 'item-card', title: 'كارت الصنف', desc: 'كشف حركة صنف خلال فترة.', needsWarehouse: true, needsOnlyInStock: false, needsDays: false, needsEmployee: false, needsItem: true, needsDates: true, needsStockCount: false },
    { key: 'stock-count', title: 'محضر جرد', desc: 'محضر جرد بالفروقات.', needsWarehouse: false, needsOnlyInStock: false, needsDays: false, needsEmployee: false, needsItem: false, needsDates: false, needsStockCount: true },
  ];

  readonly selected = signal<ReportDef>(this.reports[0]);

  // فلاتر
  warehouseId = '';
  onlyInStock = true;
  days = 90;
  from = '';
  to = '';

  // مراجع كيانات
  readonly warehouses = signal<WarehouseDto[]>([]);
  readonly stockCounts = signal<StockCountDto[]>([]);
  readonly countTypeLabel = STOCK_COUNT_TYPE_LABEL;
  readonly countStatusLabel = STOCK_COUNT_STATUS_LABEL;
  selectedStockCountId = '';

  // بحث موظف (اختياري)
  readonly empSearch = new FormControl('', { nonNullable: true });
  readonly empResults = signal<EmployeeDto[]>([]);
  readonly selectedEmployee = signal<EmployeeDto | null>(null);

  // بحث صنف (إجباري لكارت الصنف)
  readonly itemSearch = new FormControl('', { nonNullable: true });
  readonly itemResults = signal<ItemDto[]>([]);
  readonly selectedItem = signal<ItemDto | null>(null);

  // النتيجة
  readonly doc = signal<ReportDocument | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly exporting = signal<ReportExportFormat | null>(null);

  readonly canRun = computed(() => {
    const r = this.selected();
    if (r.needsItem && !this.selectedItem()) return false;
    if (r.needsStockCount && !this.selectedStockCountId) return false;
    return true;
  });

  ngOnInit(): void {
    this.vouchers.warehouses().subscribe({
      next: (ws) => this.warehouses.set(ws),
      error: () => this.warehouses.set([]),
    });

    this.empSearch.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe((term) => {
        if (!term.trim()) return this.empResults.set([]);
        this.custody.employees(term).subscribe({
          next: (list) => this.empResults.set(list.slice(0, 12)),
          error: () => this.empResults.set([]),
        });
      });

    this.itemSearch.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe((term) => {
        if (!term.trim()) return this.itemResults.set([]);
        this.itemsSvc.getItems({ search: term, page: 1, pageSize: 12 }).subscribe({
          next: (res) => this.itemResults.set(res.items),
          error: () => this.itemResults.set([]),
        });
      });
  }

  pick(r: ReportDef): void {
    this.selected.set(r);
    this.doc.set(null);
    this.error.set(null);
    // حمّل محاضر الجرد عند الحاجة أول مرة.
    if (r.needsStockCount && this.stockCounts().length === 0) {
      this.counts.list().subscribe({
        next: (list) => this.stockCounts.set(list),
        error: () => this.stockCounts.set([]),
      });
    }
  }

  chooseEmployee(e: EmployeeDto | null): void {
    this.selectedEmployee.set(e);
    this.empResults.set([]);
    if (e) this.empSearch.setValue(`${e.employeeNo} — ${e.fullNameAr}`, { emitEvent: false });
    else this.empSearch.setValue('', { emitEvent: false });
  }

  chooseItem(it: ItemDto): void {
    this.selectedItem.set(it);
    this.itemResults.set([]);
    this.itemSearch.setValue(`${it.itemCode} — ${it.nameAr}`, { emitEvent: false });
  }

  private pathAndParams(): { path: string; params: ReportParams } {
    const r = this.selected();
    const p: ReportParams = {};
    let path: string = r.key;

    if (r.needsWarehouse && this.warehouseId) p['warehouseId'] = this.warehouseId;
    if (r.needsOnlyInStock) p['onlyInStock'] = this.onlyInStock;
    if (r.needsDays) p['days'] = this.days;
    if (r.needsEmployee && this.selectedEmployee()) p['employeeId'] = this.selectedEmployee()!.id;
    if (r.needsDates) {
      if (this.from) p['from'] = this.from;
      if (this.to) p['to'] = this.to;
    }
    if (r.key === 'item-card') path = `item-card/${this.selectedItem()!.id}`;
    if (r.key === 'stock-count') path = `stock-count/${this.selectedStockCountId}`;

    return { path, params: p };
  }

  run(): void {
    if (!this.canRun()) return;
    const { path, params } = this.pathAndParams();
    this.loading.set(true);
    this.error.set(null);
    this.doc.set(null);
    this.service.fetch(path, params).subscribe({
      next: (d) => {
        this.doc.set(d);
        this.loading.set(false);
      },
      error: (err: { error?: { detail?: string; message?: string }; status?: number }) => {
        this.loading.set(false);
        this.error.set(
          err?.error?.detail ??
            err?.error?.message ??
            (err?.status === 404 ? 'لا توجد بيانات لهذا التقرير.' : 'تعذّر توليد التقرير.'),
        );
      },
    });
  }

  exportAs(format: ReportExportFormat): void {
    if (!this.canRun()) return;
    const { path, params } = this.pathAndParams();
    this.exporting.set(format);
    this.service.download(path, params, format).subscribe({
      next: (blob) => {
        this.exporting.set(null);
        this.saveBlob(blob, `${this.selected().key}.${format === 'excel' ? 'xlsx' : 'pdf'}`);
      },
      error: () => {
        this.exporting.set(null);
        this.error.set('تعذّر تصدير الملف.');
      },
    });
  }

  private saveBlob(blob: Blob, filename: string): void {
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    a.click();
    URL.revokeObjectURL(url);
  }
}
