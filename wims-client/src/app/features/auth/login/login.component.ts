import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import {
  FormBuilder,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { AuthService } from '../../../core/auth/auth.service';

/**
 * شاشة تسجيل الدخول — موصولة بـ POST /api/auth/login عبر AuthService.
 */
@Component({
  selector: 'wims-login',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly auth = inject(AuthService);

  readonly showPassword = signal(false);
  readonly loading = signal(false);
  readonly serverError = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    username: ['', [Validators.required]],
    password: ['', [Validators.required, Validators.minLength(6)]],
    remember: [true],
  });

  get username() {
    return this.form.controls.username;
  }
  get password() {
    return this.form.controls.password;
  }

  togglePassword(): void {
    this.showPassword.update((v) => !v);
  }

  onSubmit(): void {
    this.serverError.set(null);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    const { username, password, remember } = this.form.getRawValue();
    this.auth.login(username, password, remember).subscribe({
      next: () => {
        this.loading.set(false);
        const returnUrl =
          this.route.snapshot.queryParamMap.get('returnUrl') || '/dashboard';
        this.router.navigateByUrl(returnUrl);
      },
      error: (err: { error?: { detail?: string; message?: string }; status?: number }) => {
        this.loading.set(false);
        this.serverError.set(
          err?.error?.detail ??
            err?.error?.message ??
            (err?.status === 401
              ? 'اسم المستخدم أو كلمة المرور غير صحيحة.'
              : err?.status === 429
                ? 'محاولات كثيرة. انتظر دقيقة ثم أعِد المحاولة.'
                : 'تعذّر تسجيل الدخول. تأكّد من اتصال الخادم.'),
        );
      },
    });
  }
}
