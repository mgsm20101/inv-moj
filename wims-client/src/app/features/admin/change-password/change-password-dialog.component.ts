import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { problemDetail } from '../../../core/http/problem-details';
import { AdminService } from '../admin.service';
import { strongPasswordValidator } from '../password.validator';

/** تحقّق تطابق كلمة المرور الجديدة مع تأكيدها. */
function matchValidator(group: AbstractControl): ValidationErrors | null {
  const pw = group.get('newPassword')?.value;
  const confirm = group.get('confirm')?.value;
  return pw && confirm && pw !== confirm ? { mismatch: true } : null;
}

/**
 * حوار تغيير كلمة المرور الذاتية — POST /me/change-password.
 * يُفتح من قائمة المستخدم في الترويسة عبر open().
 */
@Component({
  selector: 'wims-change-password-dialog',
  standalone: true,
  imports: [ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './change-password-dialog.component.html',
})
export class ChangePasswordDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(AdminService);

  private readonly dlg =
    viewChild.required<ElementRef<HTMLDialogElement>>('dlg');

  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly done = signal(false);

  readonly form = this.fb.nonNullable.group(
    {
      currentPassword: ['', [Validators.required]],
      newPassword: ['', [Validators.required, strongPasswordValidator]],
      confirm: ['', [Validators.required]],
    },
    { validators: matchValidator },
  );

  get f() {
    return this.form.controls;
  }

  open(): void {
    this.form.reset({ currentPassword: '', newPassword: '', confirm: '' });
    this.error.set(null);
    this.done.set(false);
    this.saving.set(false);
    this.dlg().nativeElement.showModal();
  }

  submit(): void {
    this.error.set(null);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.saving.set(true);
    const { currentPassword, newPassword } = this.form.getRawValue();
    this.service.changePassword(currentPassword, newPassword).subscribe({
      next: () => {
        this.saving.set(false);
        this.done.set(true);
      },
      error: (e) => {
        this.saving.set(false);
        this.error.set(problemDetail(e, 'تعذّر تغيير كلمة المرور.'));
      },
    });
  }

  close(): void {
    this.dlg().nativeElement.close();
  }

  onCancel(e: Event): void {
    e.preventDefault();
    this.close();
  }
}
