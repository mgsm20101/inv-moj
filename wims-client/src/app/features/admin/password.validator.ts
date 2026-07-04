import { AbstractControl, ValidationErrors } from '@angular/forms';

/**
 * سياسة كلمة المرور (مطابقة للخادم): 8 أحرف على الأقل مع حرف كبير
 * وحرف صغير ورقم ورمز غير أبجدي رقمي.
 */
export function strongPasswordValidator(
  control: AbstractControl,
): ValidationErrors | null {
  const v = control.value as string;
  if (!v) return null; // required يتكفّل بالفراغ
  const errors: ValidationErrors = {};
  if (v.length < 8) errors['minlength'] = true;
  if (!/[A-Z]/.test(v)) errors['upper'] = true;
  if (!/[a-z]/.test(v)) errors['lower'] = true;
  if (!/[0-9]/.test(v)) errors['digit'] = true;
  if (!/[^a-zA-Z0-9]/.test(v)) errors['nonAlnum'] = true;
  return Object.keys(errors).length ? { weakPassword: errors } : null;
}

/** رسالة عربية موحّدة لسياسة كلمة المرور. */
export const PASSWORD_POLICY_HINT =
  'ثمانية أحرف على الأقل، وتشمل حرفاً كبيراً وصغيراً ورقماً ورمزاً.';
