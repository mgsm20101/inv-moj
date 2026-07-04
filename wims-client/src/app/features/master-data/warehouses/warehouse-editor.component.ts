import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  OnInit,
  computed,
  inject,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { problemDetail } from '../../../core/http/problem-details';
import { UserSummary } from '../../../core/models/admin.models';
import { WarehouseDto } from '../../../core/models/voucher.models';
import { WAREHOUSE_TYPES, WarehouseType } from '../master-data.models';
import { MasterDataService } from '../master-data.service';

/**
 * محرّر مخزن — حوار &lt;dialog&gt; أصلي للإنشاء والتعديل.
 * الكود غير قابل للتعديل. أمين المخزن يُختار من قائمة المستخدمين.
 */
@Component({
  selector: 'wims-warehouse-editor',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './warehouse-editor.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WarehouseEditorComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(MasterDataService);

  /** null = مخزن جديد، أو ملخّص مخزن قائم للتعديل. */
  readonly warehouse = input.required<WarehouseDto | null>();
  readonly closed = output<boolean>();

  private readonly dlg = viewChild.required<ElementRef<HTMLDialogElement>>('dlg');

  readonly types = WAREHOUSE_TYPES;
  readonly isNew = computed(() => this.warehouse() === null);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly users = signal<UserSummary[]>([]);

  readonly form = this.fb.nonNullable.group({
    code: ['', [Validators.required, Validators.maxLength(10)]],
    nameAr: ['', [Validators.required, Validators.maxLength(150)]],
    warehouseType: [WarehouseType.Main, [Validators.required]],
    region: [''],
    keeperUserId: ['', [Validators.required]],
    usesLocations: [false],
  });

  get f() {
    return this.form.controls;
  }

  ngOnInit(): void {
    this.loading.set(true);
    this.service.getUsers().subscribe({
      next: (u) => {
        this.users.set(u);
        const w = this.warehouse();
        if (w) this.loadDetail(w.id);
        else {
          this.loading.set(false);
          this.dlg().nativeElement.showModal();
        }
      },
      error: (e) => {
        this.error.set(problemDetail(e, 'تعذّر تحميل قائمة المستخدمين (تحتاج صلاحية عرض المستخدمين).'));
        this.loading.set(false);
        this.dlg().nativeElement.showModal();
      },
    });
  }

  private loadDetail(id: string): void {
    this.service.getWarehouse(id).subscribe({
      next: (w) => {
        this.form.patchValue({
          code: w.code,
          nameAr: w.nameAr,
          warehouseType: w.warehouseType,
          region: w.region ?? '',
          keeperUserId: w.keeperUserId,
          usesLocations: w.usesLocations,
        });
        this.f.code.disable(); // الكود ثابت
        this.loading.set(false);
        this.dlg().nativeElement.showModal();
      },
      error: (e) => {
        this.error.set(problemDetail(e, 'تعذّر تحميل بيانات المخزن.'));
        this.loading.set(false);
        this.dlg().nativeElement.showModal();
      },
    });
  }

  submit(): void {
    this.error.set(null);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.saving.set(true);
    const raw = this.form.getRawValue();
    const region = raw.region.trim() || null;

    if (this.isNew()) {
      this.service
        .createWarehouse({
          code: raw.code,
          nameAr: raw.nameAr,
          warehouseType: raw.warehouseType,
          region,
          keeperUserId: raw.keeperUserId,
          usesLocations: raw.usesLocations,
        })
        .subscribe({
          next: () => this.finish(),
          error: (e) => this.fail(e, 'تعذّر إنشاء المخزن.'),
        });
    } else {
      this.service
        .updateWarehouse(this.warehouse()!.id, {
          nameAr: raw.nameAr,
          warehouseType: raw.warehouseType,
          region,
          keeperUserId: raw.keeperUserId,
          usesLocations: raw.usesLocations,
        })
        .subscribe({
          next: () => this.finish(),
          error: (e) => this.fail(e, 'تعذّر حفظ بيانات المخزن.'),
        });
    }
  }

  private finish(): void {
    this.saving.set(false);
    this.dlg().nativeElement.close();
    this.closed.emit(true);
  }

  private fail(e: unknown, fallback: string): void {
    this.saving.set(false);
    this.error.set(problemDetail(e, fallback));
  }

  cancel(): void {
    this.dlg().nativeElement.close();
    this.closed.emit(false);
  }

  onDialogCancel(e: Event): void {
    e.preventDefault();
    this.cancel();
  }
}
