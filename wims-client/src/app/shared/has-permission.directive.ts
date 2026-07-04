import {
  Directive,
  TemplateRef,
  ViewContainerRef,
  effect,
  inject,
  input,
} from '@angular/core';
import { AuthService } from '../core/auth/auth.service';

/**
 * توجيه هيكلي يعرض محتواه فقط إذا كان المستخدم يملك الصلاحية (أو أيّ صلاحية من قائمة).
 * الاستخدام:
 *   <button *appHasPermission="'Items.Manage'"> … </button>
 *   <button *appHasPermission="['Vouchers.Approve','Approvals.Act']"> … </button>
 */
@Directive({
  selector: '[appHasPermission]',
  standalone: true,
})
export class HasPermissionDirective {
  private readonly auth = inject(AuthService);
  private readonly tpl = inject(TemplateRef<unknown>);
  private readonly vcr = inject(ViewContainerRef);

  /** مفتاح صلاحية واحد أو مصفوفة مفاتيح (أيّ منها يكفي). */
  readonly appHasPermission = input.required<string | string[]>();

  private rendered = false;

  constructor() {
    // إشارات AuthService تفاعلية → يُعاد التقييم عند تغيّر الجلسة/الصلاحيات.
    // allowSignalWrites: إدراج/إزالة عرض مضمَّن (createEmbeddedView/clear) يُلوِّث
    // إشارات استعلامات viewChild الداخلية في أي مكوّن مضيف يملك مثل هذا الاستعلام
    // (مثل ConfirmDialogComponent عبر viewChild.required) — بدون هذا الخيار يرمي
    // Angular NG0600 ويفشل إنشاء العرض بصمت (الزر لا يظهر، بلا أي خطأ ظاهر للمستخدم).
    effect(
      () => {
        const req = this.appHasPermission();
        const keys = Array.isArray(req) ? req : [req];
        const allowed = keys.length > 0 && this.auth.hasAnyPermission(keys);

        if (allowed && !this.rendered) {
          this.vcr.createEmbeddedView(this.tpl);
          this.rendered = true;
        } else if (!allowed && this.rendered) {
          this.vcr.clear();
          this.rendered = false;
        }
      },
      { allowSignalWrites: true },
    );
  }
}
