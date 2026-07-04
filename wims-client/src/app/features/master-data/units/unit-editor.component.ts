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
import { UnitDto } from '../../../core/models/catalog.models';
import { MasterDataService } from '../master-data.service';

/**
 * محرّر وحدة قياس — حوار &lt;dialog&gt; أصلي. الرمز غير قابل للتعديل.
 * التعديل يُعبَّأ من صفّ القائمة مباشرةً (لا حاجة لجلب تفاصيل).
 */
@Component({
  selector: 'wims-unit-editor',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './unit-editor.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UnitEditorComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(MasterDataService);

  /** null = وحدة جديدة، أو الوحدة القائمة للتعديل. */
  readonly unit = input.required<UnitDto | null>();
  readonly closed = output<boolean>();

  private readonly dlg = viewChild.required<ElementRef<HTMLDialogElement>>('dlg');

  readonly isNew = computed(() => this.unit() === null);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    code: ['', [Validators.required, Validators.maxLength(10)]],
    nameAr: ['', [Validators.required, Validators.maxLength(50)]],
    isBaseUnit: [true],
  });

  get f() {
    return this.form.controls;
  }

  ngOnInit(): void {
    const u = this.unit();
    if (u) {
      this.form.patchValue({ code: u.code, nameAr: u.nameAr, isBaseUnit: u.isBaseUnit });
      this.f.code.disable(); // الرمز ثابت
    }
    this.dlg().nativeElement.showModal();
  }

  submit(): void {
    this.error.set(null);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.saving.set(true);
    const raw = this.form.getRawValue();

    if (this.isNew()) {
      this.service
        .createUnit({ code: raw.code, nameAr: raw.nameAr, isBaseUnit: raw.isBaseUnit })
        .subscribe({
          next: () => this.finish(),
          error: (e) => this.fail(e, 'تعذّر إنشاء الوحدة.'),
        });
    } else {
      this.service
        .updateUnit(this.unit()!.id, { nameAr: raw.nameAr, isBaseUnit: raw.isBaseUnit })
        .subscribe({
          next: () => this.finish(),
          error: (e) => this.fail(e, 'تعذّر حفظ الوحدة.'),
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
