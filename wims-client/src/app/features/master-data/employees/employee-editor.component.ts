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
import { EmployeeDto } from '../../../core/models/custody.models';
import { MasterDataService } from '../master-data.service';

/**
 * محرّر موظف — حوار &lt;dialog&gt; أصلي للإنشاء والتعديل.
 * الرقم الوظيفي والهوية الوطنية غير قابلين للتعديل. الربط بحساب مستخدم اختياري.
 */
@Component({
  selector: 'wims-employee-editor',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './employee-editor.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EmployeeEditorComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(MasterDataService);

  /** null = موظف جديد، أو ملخّص موظف قائم للتعديل. */
  readonly employee = input.required<EmployeeDto | null>();
  readonly closed = output<boolean>();

  private readonly dlg = viewChild.required<ElementRef<HTMLDialogElement>>('dlg');

  readonly isNew = computed(() => this.employee() === null);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly users = signal<UserSummary[]>([]);

  readonly form = this.fb.nonNullable.group({
    employeeNo: ['', [Validators.required, Validators.maxLength(20)]],
    nationalId: ['', [Validators.required, Validators.pattern(/^\d{10}$/)]],
    fullNameAr: ['', [Validators.required, Validators.maxLength(150)]],
    fullNameEn: ['', [Validators.maxLength(150)]],
    department: ['', [Validators.required, Validators.maxLength(120)]],
    jobTitle: [''],
    costCenter: ['', [Validators.required, Validators.maxLength(30)]],
    email: ['', [Validators.email]],
    phone: [''],
    userId: [''],
  });

  get f() {
    return this.form.controls;
  }

  ngOnInit(): void {
    this.loading.set(true);
    this.service.getUsers().subscribe({
      next: (u) => {
        this.users.set(u);
        const e = this.employee();
        if (e) this.loadDetail(e.id);
        else {
          this.loading.set(false);
          this.dlg().nativeElement.showModal();
        }
      },
      // قائمة المستخدمين اختيارية للربط — لا نمنع الحوار عند تعذّرها.
      error: () => {
        const e = this.employee();
        if (e) this.loadDetail(e.id);
        else {
          this.loading.set(false);
          this.dlg().nativeElement.showModal();
        }
      },
    });
  }

  private loadDetail(id: string): void {
    this.service.getEmployee(id).subscribe({
      next: (e) => {
        this.form.patchValue({
          employeeNo: e.employeeNo,
          nationalId: e.nationalId,
          fullNameAr: e.fullNameAr,
          fullNameEn: e.fullNameEn ?? '',
          department: e.department,
          jobTitle: e.jobTitle ?? '',
          costCenter: e.costCenter,
          email: e.email ?? '',
          phone: e.phone ?? '',
          userId: e.userId ?? '',
        });
        this.f.employeeNo.disable(); // ثابتان
        this.f.nationalId.disable();
        this.loading.set(false);
        this.dlg().nativeElement.showModal();
      },
      error: (err) => {
        this.error.set(problemDetail(err, 'تعذّر تحميل بيانات الموظف.'));
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
      fullNameAr: raw.fullNameAr,
      fullNameEn: raw.fullNameEn.trim() || null,
      department: raw.department,
      jobTitle: raw.jobTitle.trim() || null,
      costCenter: raw.costCenter,
      email: raw.email.trim() || null,
      phone: raw.phone.trim() || null,
      userId: raw.userId || null,
    };

    if (this.isNew()) {
      this.service
        .createEmployee({
          employeeNo: raw.employeeNo,
          nationalId: raw.nationalId,
          ...common,
        })
        .subscribe({
          next: () => this.finish(),
          error: (e) => this.fail(e, 'تعذّر إنشاء الموظف.'),
        });
    } else {
      this.service.updateEmployee(this.employee()!.id, common).subscribe({
        next: () => this.finish(),
        error: (e) => this.fail(e, 'تعذّر حفظ بيانات الموظف.'),
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
