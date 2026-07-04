import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import {
  FormArray,
  FormBuilder,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import { VouchersService } from '../vouchers.service';
import { ItemsService } from '../../items/items.service';
import { ItemDto } from '../../../core/models/catalog.models';
import { CustodyService } from '../../custody/custody.service';
import { EmployeeDto, EmployeeStatus } from '../../../core/models/custody.models';
import {
  ADJUSTMENT_TYPE_LABEL,
  AdjustmentType,
  CreateVoucherCommand,
  SupplierDto,
  VOUCHER_TYPE_LABEL,
  VoucherType,
  WarehouseDto,
} from '../../../core/models/voucher.models';

@Component({
  selector: 'wims-voucher-create',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './voucher-create.component.html',
  styleUrl: './voucher-create.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VoucherCreateComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(VouchersService);
  private readonly itemsService = inject(ItemsService);
  private readonly custodyService = inject(CustodyService);
  private readonly router = inject(Router);

  readonly warehouses = signal<WarehouseDto[]>([]);
  readonly suppliers = signal<SupplierDto[]>([]);
  readonly items = signal<ItemDto[]>([]);
  readonly employees = signal<EmployeeDto[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly serverError = signal<string | null>(null);

  readonly typeOptions = Object.entries(VOUCHER_TYPE_LABEL).map(([v, label]) => ({
    value: Number(v) as VoucherType,
    label,
  }));
  readonly adjustmentOptions = Object.entries(ADJUSTMENT_TYPE_LABEL).map(
    ([v, label]) => ({ value: Number(v) as AdjustmentType, label }),
  );
  readonly VoucherType = VoucherType;

  readonly form = this.fb.nonNullable.group({
    voucherType: [VoucherType.Issue, [Validators.required]],
    warehouseId: ['', [Validators.required]],
    toWarehouseId: [''],
    supplierId: [''],
    adjustmentType: [null as AdjustmentType | null],
    referenceNo: [''],
    costCenter: ['', [Validators.required]],
    requestingDept: [''],
    reason: [''],
    recipientEmployeeId: [''],
    notes: [''],
    lines: this.fb.array([this.newLine()]),
  });

  readonly currentType = signal<VoucherType>(VoucherType.Issue);
  readonly isTransfer = computed(() => this.currentType() === VoucherType.Transfer);
  readonly isReceipt = computed(() => this.currentType() === VoucherType.Receipt);
  readonly isIssue = computed(() => this.currentType() === VoucherType.Issue);
  readonly isAdjustment = computed(
    () => this.currentType() === VoucherType.Adjustment,
  );
  readonly showUnitCost = computed(() => {
    const t = this.currentType();
    return t === VoucherType.Receipt || t === VoucherType.Adjustment;
  });

  get lines(): FormArray {
    return this.form.controls.lines;
  }

  ngOnInit(): void {
    this.form.controls.voucherType.valueChanges.subscribe((t) => {
      this.currentType.set(t);
      const to = this.form.controls.toWarehouseId;
      const adj = this.form.controls.adjustmentType;
      const sup = this.form.controls.supplierId;
      const cc = this.form.controls.costCenter;
      to.clearValidators();
      adj.clearValidators();
      sup.clearValidators();
      cc.clearValidators();
      if (t === VoucherType.Transfer) to.setValidators([Validators.required]);
      if (t === VoucherType.Adjustment) adj.setValidators([Validators.required]);
      // المورّد إلزامي في الاستلام، ومركز التكلفة إلزامي في الصرف (تطابق قواعد الـbackend).
      if (t === VoucherType.Receipt) sup.setValidators([Validators.required]);
      if (t === VoucherType.Issue) cc.setValidators([Validators.required]);
      to.updateValueAndValidity();
      adj.updateValueAndValidity();
      sup.updateValueAndValidity();
      cc.updateValueAndValidity();
    });

    forkJoin({
      warehouses: this.service.warehouses(),
      suppliers: this.service.suppliers(),
      items: this.itemsService.getItems({ page: 1, pageSize: 200, isActive: true }),
      employees: this.custodyService.employees(),
    }).subscribe({
      next: ({ warehouses, suppliers, items, employees }) => {
        this.warehouses.set(warehouses.filter((w) => w.isActive));
        this.suppliers.set(suppliers.filter((s) => s.isActive));
        this.items.set(items.items);
        this.employees.set(
          employees.filter((e) => e.status === EmployeeStatus.Active),
        );
        this.loading.set(false);
      },
      error: () => {
        this.serverError.set('تعذّر تحميل المخازن والموردين والأصناف.');
        this.loading.set(false);
      },
    });
  }

  private newLine() {
    return this.fb.nonNullable.group({
      itemId: ['', [Validators.required]],
      qty: [1, [Validators.required, Validators.min(0.001)]],
      unitCost: [0, [Validators.min(0)]],
      batchNo: [''],
      serialNo: [''],
      expiryDate: [''],
    });
  }

  addLine(): void {
    this.lines.push(this.newLine());
  }

  removeLine(i: number): void {
    if (this.lines.length > 1) this.lines.removeAt(i);
  }

  submit(): void {
    this.serverError.set(null);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.saving.set(true);
    const raw = this.form.getRawValue();
    const cmd: CreateVoucherCommand = {
      voucherType: raw.voucherType,
      warehouseId: raw.warehouseId,
      toWarehouseId: this.isTransfer() ? raw.toWarehouseId || null : null,
      supplierId: this.isReceipt() ? raw.supplierId || null : null,
      adjustmentType: this.isAdjustment() ? raw.adjustmentType : null,
      referenceNo: raw.referenceNo || null,
      costCenter: raw.costCenter || null,
      requestingDept: raw.requestingDept || null,
      reason: raw.reason || null,
      recipientEmployeeId: this.isIssue() ? raw.recipientEmployeeId || null : null,
      notes: raw.notes || null,
      lines: raw.lines.map((l) => ({
        itemId: l.itemId,
        qty: l.qty,
        unitCost: this.showUnitCost() ? l.unitCost : null,
        batchNo: l.batchNo || null,
        serialNo: l.serialNo || null,
        expiryDate: l.expiryDate || null,
      })),
    };

    this.service.create(cmd).subscribe({
      next: (id) => this.router.navigate(['/movements', id]),
      error: (err: { error?: { message?: string } }) => {
        this.serverError.set(
          err?.error?.message ?? 'تعذّر حفظ المستند. تحقّق من الحقول ثم أعِد المحاولة.',
        );
        this.saving.set(false);
      },
    });
  }
}
