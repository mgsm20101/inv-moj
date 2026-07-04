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
import { SupplierDto } from '../master-data.models';
import { MasterDataService } from '../master-data.service';

/**
 * محرّر مورّد — حوار <dialog> أصلي للإنشاء والتعديل.
 * الكود غير قابل للتعديل. حالة التفعيل تُضبط عند التعديل.
 */
@Component({
  selector: 'wims-supplier-editor',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './supplier-editor.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SupplierEditorComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(MasterDataService);

  /** null = مورّد جديد، أو ملخّص مورّد قائم للتعديل. */
  readonly supplier = input.required<SupplierDto | null>();
  readonly closed = output<boolean>();

  private readonly dlg = viewChild.required<ElementRef<HTMLDialogElement>>('dlg');

  readonly isNew = computed(() => this.supplier() === null);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    code: ['', [Validators.required, Validators.maxLength(20)]],
    nameAr: ['', [Validators.required, Validators.maxLength(200)]],
    nameEn: [''],
    taxNumber: [''],
    commercialReg: [''],
    contactPerson: [''],
    phone: [''],
    email: ['', [Validators.email]],
    address: [''],
    isActive: [true],
  });

  get f() {
    return this.form.controls;
  }

  ngOnInit(): void {
    const s = this.supplier();
    if (s) {
      this.loadDetail(s.id);
    } else {
      this.dlg().nativeElement.showModal();
    }
  }

  private loadDetail(id: string): void {
    this.loading.set(true);
    this.service.getSupplier(id).subscribe({
      next: (s) => {
        this.form.patchValue({
          code: s.code,
          nameAr: s.nameAr,
          nameEn: s.nameEn ?? '',
          taxNumber: s.taxNumber ?? '',
          commercialReg: s.commercialReg ?? '',
          contactPerson: s.contactPerson ?? '',
          phone: s.phone ?? '',
          email: s.email ?? '',
          address: s.address ?? '',
          isActive: s.isActive,
        });
        this.f.code.disable(); // الكود ثابت
        this.loading.set(false);
        this.dlg().nativeElement.showModal();
      },
      error: (e) => {
        this.error.set(problemDetail(e, 'تعذّر تحميل بيانات المورّد.'));
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
    const common = {
      nameAr: raw.nameAr,
      nameEn: raw.nameEn.trim() || null,
      taxNumber: raw.taxNumber.trim() || null,
      commercialReg: raw.commercialReg.trim() || null,
      contactPerson: raw.contactPerson.trim() || null,
      phone: raw.phone.trim() || null,
      email: raw.email.trim() || null,
      address: raw.address.trim() || null,
    };

    if (this.isNew()) {
      this.service
        .createSupplier({ code: raw.code, ...common })
        .subscribe({
          next: () => this.finish(),
          error: (e) => this.fail(e, 'تعذّر إنشاء المورّد.'),
        });
    } else {
      this.service
        .updateSupplier(this.supplier()!.id, { ...common, isActive: raw.isActive })
        .subscribe({
          next: () => this.finish(),
          error: (e) => this.fail(e, 'تعذّر حفظ بيانات المورّد.'),
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
