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
import { CategoryDto } from '../../../core/models/catalog.models';
import { MasterDataService } from '../master-data.service';

/**
 * محرّر تصنيف — حوار &lt;dialog&gt; أصلي. الكود والأب ثابتان بعد الإنشاء.
 * قائمة الأصناف الأب تُحمَّل للإنشاء فقط.
 */
@Component({
  selector: 'wims-category-editor',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './category-editor.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CategoryEditorComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(MasterDataService);

  /** null = تصنيف جديد، أو التصنيف القائم للتعديل. */
  readonly category = input.required<CategoryDto | null>();
  readonly closed = output<boolean>();

  private readonly dlg = viewChild.required<ElementRef<HTMLDialogElement>>('dlg');

  readonly isNew = computed(() => this.category() === null);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly parents = signal<CategoryDto[]>([]);

  readonly form = this.fb.nonNullable.group({
    code: ['', [Validators.required, Validators.maxLength(10)]],
    nameAr: ['', [Validators.required, Validators.maxLength(150)]],
    nameEn: ['', [Validators.maxLength(150)]],
    parentId: [''],
    sortOrder: [0, [Validators.required]],
  });

  get f() {
    return this.form.controls;
  }

  ngOnInit(): void {
    const c = this.category();
    if (c) {
      // تعديل: تعبئة من الصفّ، الكود والأب ثابتان.
      this.form.patchValue({
        code: c.code,
        nameAr: c.nameAr,
        nameEn: c.nameEn ?? '',
        parentId: c.parentId ?? '',
        sortOrder: c.sortOrder,
      });
      this.f.code.disable();
      this.f.parentId.disable();
      this.dlg().nativeElement.showModal();
      return;
    }

    // إنشاء: تحميل قائمة الأصناف الأب المحتملة.
    this.loading.set(true);
    this.service.getCategories().subscribe({
      next: (list) => {
        this.parents.set(list);
        this.loading.set(false);
        this.dlg().nativeElement.showModal();
      },
      error: (e) => {
        this.error.set(problemDetail(e, 'تعذّر تحميل التصنيفات.'));
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
    const nameEn = raw.nameEn.trim() || null;

    if (this.isNew()) {
      this.service
        .createCategory({
          code: raw.code,
          nameAr: raw.nameAr,
          nameEn,
          parentId: raw.parentId || null,
          sortOrder: raw.sortOrder,
        })
        .subscribe({
          next: () => this.finish(),
          error: (e) => this.fail(e, 'تعذّر إنشاء التصنيف.'),
        });
    } else {
      this.service
        .updateCategory(this.category()!.id, {
          nameAr: raw.nameAr,
          nameEn,
          sortOrder: raw.sortOrder,
        })
        .subscribe({
          next: () => this.finish(),
          error: (e) => this.fail(e, 'تعذّر حفظ التصنيف.'),
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
