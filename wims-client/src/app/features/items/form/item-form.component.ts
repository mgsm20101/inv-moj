import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Observable, forkJoin } from 'rxjs';
import { ItemsService } from '../items.service';
import { environment } from '../../../../environments/environment';
import {
  CategoryDto,
  ITEM_TYPE_LABEL,
  ItemType,
  UnitDto,
} from '../../../core/models/catalog.models';

/** الحد الأدنى ≤ نقطة الطلب ≤ الحد الأقصى (BR-07). */
function stockOrderValidator(group: AbstractControl): ValidationErrors | null {
  const min = group.get('minStock')?.value ?? 0;
  const reorder = group.get('reorderPoint')?.value ?? 0;
  const max = group.get('maxStock')?.value;
  if (reorder < min) return { reorderBelowMin: true };
  if (max != null && max !== '' && Number(max) < reorder)
    return { maxBelowReorder: true };
  return null;
}

@Component({
  selector: 'wims-item-form',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './item-form.component.html',
  styleUrl: './item-form.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ItemFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(ItemsService);
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly categories = signal<CategoryDto[]>([]);
  readonly units = signal<UnitDto[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly serverError = signal<string | null>(null);
  readonly editId = signal<string | null>(null);

  readonly itemTypes = Object.entries(ITEM_TYPE_LABEL).map(([v, label]) => ({
    value: Number(v) as ItemType,
    label,
  }));
  readonly ItemType = ItemType;

  readonly form = this.fb.nonNullable.group(
    {
      itemCode: [''],
      nameAr: ['', [Validators.required, Validators.maxLength(200)]],
      nameEn: [''],
      barcode: [''],
      categoryId: ['', [Validators.required]],
      itemType: [ItemType.Consumable, [Validators.required]],
      baseUnitId: ['', [Validators.required]],
      tracksBatch: [false],
      tracksExpiry: [false],
      tracksSerial: [false],
      minStock: [0, [Validators.required, Validators.min(0)]],
      reorderPoint: [0, [Validators.required, Validators.min(0)]],
      maxStock: [null as number | null],
      reorderQty: [null as number | null],
      hazardClass: [''],
      storageConditions: [''],
      shelfLifeDays: [null as number | null],
    },
    { validators: stockOrderValidator },
  );

  readonly currentType = signal<ItemType>(ItemType.Consumable);
  readonly isHazardous = computed(() => this.currentType() === ItemType.Hazardous);
  readonly isPerishable = computed(() => this.currentType() === ItemType.Perishable);

  get f() {
    return this.form.controls;
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    this.editId.set(id);

    this.form.controls.itemType.valueChanges.subscribe((t) => {
      this.currentType.set(t);
      this.applyTypeRules(t);
    });

    forkJoin({
      categories: this.service.getCategories(),
      units: this.service.getUnits(),
    }).subscribe({
      next: ({ categories, units }) => {
        this.categories.set(categories.filter((c) => c.isLeaf && c.isActive));
        this.units.set(units.filter((u) => u.isActive));
        if (id) this.loadItem(id);
        else this.loading.set(false);
      },
      error: () => {
        this.serverError.set('تعذّر تحميل بيانات التصنيفات والوحدات.');
        this.loading.set(false);
      },
    });
  }

  private applyTypeRules(t: ItemType): void {
    const hazard = this.form.controls.hazardClass;
    const shelf = this.form.controls.shelfLifeDays;
    hazard.clearValidators();
    shelf.clearValidators();
    if (t === ItemType.Hazardous) hazard.setValidators([Validators.required]);
    if (t === ItemType.Perishable) {
      shelf.setValidators([Validators.required, Validators.min(1)]);
      this.form.controls.tracksExpiry.setValue(true);
    }
    hazard.updateValueAndValidity();
    shelf.updateValueAndValidity();
  }

  private loadItem(id: string): void {
    this.service.getItem(id).subscribe({
      next: (item) => {
        this.form.patchValue({
          itemCode: item.itemCode,
          nameAr: item.nameAr,
          nameEn: item.nameEn ?? '',
          barcode: item.barcode ?? '',
          categoryId: item.categoryId,
          itemType: item.itemType,
          baseUnitId: item.baseUnitId,
          tracksBatch: item.tracksBatch,
          tracksExpiry: item.tracksExpiry,
          tracksSerial: item.tracksSerial,
          minStock: item.minStock,
          reorderPoint: item.reorderPoint,
          maxStock: item.maxStock,
          hazardClass: item.hazardClass ?? '',
        });
        // في التعديل: الكود/التصنيف/النوع/الوحدة غير قابلة للتغيير
        this.form.controls.itemCode.disable();
        this.form.controls.categoryId.disable();
        this.form.controls.itemType.disable();
        this.form.controls.baseUnitId.disable();
        this.loading.set(false);
      },
      error: () => {
        this.serverError.set('الصنف غير موجود.');
        this.loading.set(false);
      },
    });
  }

  submit(): void {
    this.serverError.set(null);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.saving.set(true);
    const raw = this.form.getRawValue();
    const id = this.editId();

    const req: Observable<unknown> = id
      ? this.http.put<void>(`${environment.apiBaseUrl}/items/${id}`, {
          nameAr: raw.nameAr,
          nameEn: raw.nameEn || null,
          description: null,
          barcode: raw.barcode || null,
          minStock: raw.minStock,
          maxStock: raw.maxStock,
          reorderPoint: raw.reorderPoint,
          reorderQty: raw.reorderQty,
          hazardClass: raw.hazardClass || null,
          storageConditions: raw.storageConditions || null,
          shelfLifeDays: raw.shelfLifeDays,
        })
      : this.http.post<string>(`${environment.apiBaseUrl}/items`, {
          itemCode: raw.itemCode || null,
          barcode: raw.barcode || null,
          nameAr: raw.nameAr,
          nameEn: raw.nameEn || null,
          categoryId: raw.categoryId,
          itemType: raw.itemType,
          baseUnitId: raw.baseUnitId,
          tracksBatch: raw.tracksBatch,
          tracksExpiry: raw.tracksExpiry,
          tracksSerial: raw.tracksSerial,
          minStock: raw.minStock,
          maxStock: raw.maxStock,
          reorderPoint: raw.reorderPoint,
          reorderQty: raw.reorderQty,
          hazardClass: raw.hazardClass || null,
          storageConditions: raw.storageConditions || null,
          shelfLifeDays: raw.shelfLifeDays,
          isStockItem: true,
        });

    req.subscribe({
      next: () => this.router.navigateByUrl('/items'),
      error: (err: { error?: { message?: string } }) => {
        this.serverError.set(
          err?.error?.message ?? 'تعذّر حفظ الصنف. تحقّق من الحقول ثم أعِد المحاولة.',
        );
        this.saving.set(false);
      },
    });
  }
}
