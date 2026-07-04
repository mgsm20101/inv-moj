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
import {
  FormBuilder,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { problemDetail } from '../../../core/http/problem-details';
import { RoleSummary } from '../../../core/models/admin.models';
import { strongPasswordValidator } from '../password.validator';
import { AdminService } from '../admin.service';

/**
 * محرّر مستخدم — حوار <dialog> أصلي يغطّي الإنشاء والتعديل:
 * إنشاء (اسم دخول + اسم كامل + بريد + كلمة مرور + أدوار)، تعديل (اسم/بريد)،
 * تبديل التفعيل، تصفير كلمة المرور، وإسناد الأدوار. أخطاء الخادم ProblemDetails.detail.
 */
@Component({
  selector: 'wims-user-editor',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './user-editor.component.html',
  styles: [`
    .photo-pick { display: flex; align-items: center; gap: var(--space-md); }
    .photo-pick__img, .photo-pick__ph {
      width: 64px; height: 64px; border-radius: 50%; flex: none;
      object-fit: cover; border: 1px solid var(--border);
    }
    .photo-pick__ph {
      display: grid; place-items: center;
      background: var(--justice-soft); color: #14503f;
    }
    .photo-pick__ph svg { width: 32px; height: 32px; }
    .photo-pick__btn { cursor: pointer; }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserEditorComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(AdminService);

  /** null = مستخدم جديد، أو معرّف مستخدم قائم للتعديل. */
  readonly userId = input.required<string | null>();
  readonly closed = output<boolean>();

  private readonly dlg =
    viewChild.required<ElementRef<HTMLDialogElement>>('dlg');

  readonly isNew = computed(() => this.userId() === null);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly isActive = signal(true);
  readonly userName = signal('');

  readonly roles = signal<RoleSummary[]>([]);
  readonly selectedRoles = signal<Set<string>>(new Set());
  readonly showResetPw = signal(false);

  /** معاينة الصورة (ObjectURL) والملف المختار للرفع. */
  readonly photoPreview = signal<string | null>(null);
  private photoFile: File | null = null;

  readonly form = this.fb.nonNullable.group({
    userName: ['', [Validators.required, Validators.maxLength(64)]],
    fullName: ['', [Validators.required, Validators.maxLength(200)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, strongPasswordValidator]],
  });

  readonly resetPwCtrl = this.fb.nonNullable.control('', [
    Validators.required,
    strongPasswordValidator,
  ]);

  get f() {
    return this.form.controls;
  }

  ngOnInit(): void {
    this.loading.set(true);
    this.service.getRoles().subscribe({
      next: (r) => {
        this.roles.set(r);
        const id = this.userId();
        if (id) this.loadUser(id);
        else {
          this.loading.set(false);
          this.dlg().nativeElement.showModal();
        }
      },
      error: (e) => {
        this.error.set(problemDetail(e, 'تعذّر تحميل الأدوار.'));
        this.loading.set(false);
        this.dlg().nativeElement.showModal();
      },
    });
  }

  private loadUser(id: string): void {
    this.service.getUser(id).subscribe({
      next: (u) => {
        this.userName.set(u.userName);
        this.isActive.set(u.isActive);
        this.selectedRoles.set(new Set(u.roleIds));
        this.form.patchValue({
          userName: u.userName,
          fullName: u.fullName,
          email: u.email,
        });
        // في التعديل: اسم الدخول وكلمة المرور لا يُعدَّلان عبر هذا النموذج.
        this.f.userName.disable();
        this.f.password.disable();
        if (u.hasPhoto) {
          this.service.getUserPhoto(id).subscribe({
            next: (blob) => this.photoPreview.set(URL.createObjectURL(blob)),
            error: () => {},
          });
        }
        this.loading.set(false);
        this.dlg().nativeElement.showModal();
      },
      error: (e) => {
        this.error.set(problemDetail(e, 'تعذّر تحميل بيانات المستخدم.'));
        this.loading.set(false);
        this.dlg().nativeElement.showModal();
      },
    });
  }

  toggleRole(id: string): void {
    this.selectedRoles.update((s) => {
      const next = new Set(s);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  }

  isRoleSelected(id: string): boolean {
    return this.selectedRoles().has(id);
  }

  onPhotoSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    this.photoFile = file;
    this.photoPreview.set(URL.createObjectURL(file));
  }

  /** بعد حفظ بيانات المستخدم: يرفع الصورة إن اختِيرت ثم يُغلق. */
  private afterSave(id: string): void {
    if (this.photoFile) {
      this.service.uploadUserPhoto(id, this.photoFile).subscribe({
        next: () => this.finish(),
        error: (e) => this.fail(e, 'حُفظ المستخدم لكن تعذّر رفع الصورة.'),
      });
    } else {
      this.finish();
    }
  }

  // ---- الحفظ ----
  submit(): void {
    this.error.set(null);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.saving.set(true);
    const raw = this.form.getRawValue();
    const roleIds = [...this.selectedRoles()];

    if (this.isNew()) {
      this.service
        .createUser({
          userName: raw.userName,
          fullName: raw.fullName,
          email: raw.email,
          password: raw.password,
          roleIds,
        })
        .subscribe({
          next: (id) => this.afterSave(id),
          error: (e) => this.fail(e, 'تعذّر إنشاء المستخدم.'),
        });
    } else {
      const id = this.userId()!;
      // تعديل البيانات ثم مزامنة الأدوار.
      this.service
        .updateUser(id, { fullName: raw.fullName, email: raw.email })
        .subscribe({
          next: () =>
            this.service.assignRoles(id, roleIds).subscribe({
              next: () => this.afterSave(id),
              error: (e) => this.fail(e, 'حُفظت البيانات لكن تعذّر إسناد الأدوار.'),
            }),
          error: (e) => this.fail(e, 'تعذّر حفظ بيانات المستخدم.'),
        });
    }
  }

  toggleActive(): void {
    const id = this.userId();
    if (!id) return;
    const next = !this.isActive();
    this.error.set(null);
    this.service.setUserActive(id, next).subscribe({
      next: () => this.isActive.set(next),
      error: (e) => this.error.set(problemDetail(e, 'تعذّر تغيير حالة التفعيل.')),
    });
  }

  submitReset(): void {
    const id = this.userId();
    if (!id || this.resetPwCtrl.invalid) {
      this.resetPwCtrl.markAsTouched();
      return;
    }
    this.error.set(null);
    this.saving.set(true);
    this.service.resetPassword(id, this.resetPwCtrl.value).subscribe({
      next: () => {
        this.saving.set(false);
        this.showResetPw.set(false);
        this.resetPwCtrl.reset('');
      },
      error: (e) => this.fail(e, 'تعذّر تصفير كلمة المرور.'),
    });
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
