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
import { Permission } from '../../../core/models/admin.models';
import { AdminService } from '../admin.service';

interface PermModule {
  module: string;
  items: Permission[];
}

/**
 * محرّر دور — حوار <dialog> أصلي: اسم + وصف + مصفوفة صلاحيات (شبكة مربّعات
 * مجمّعة حسب الوحدة). عند الإنشاء تُرسل الصلاحيات ضمن الإنشاء؛ عند التعديل
 * تُحفظ البيانات ثم تُزامن الصلاحيات عبر PUT /roles/{id}/permissions.
 * الأدوار النظامية (isSystem) للقراءة فقط.
 */
@Component({
  selector: 'wims-role-editor',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './role-editor.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RoleEditorComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(AdminService);

  /** null = دور جديد، أو معرّف دور قائم للتعديل. */
  readonly roleId = input.required<string | null>();
  /** كل الصلاحيات المتاحة (تأتي محمّلة من الشاشة الأم). */
  readonly permissions = input.required<Permission[]>();
  readonly closed = output<boolean>();

  private readonly dlg =
    viewChild.required<ElementRef<HTMLDialogElement>>('dlg');

  readonly isNew = computed(() => this.roleId() === null);
  readonly isSystem = signal(false);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  readonly selected = signal<Set<string>>(new Set());

  /** الصلاحيات مجمّعة حسب الوحدة للعرض في المصفوفة. */
  readonly modules = computed<PermModule[]>(() => {
    const byModule = new Map<string, Permission[]>();
    for (const p of this.permissions()) {
      const list = byModule.get(p.module) ?? [];
      list.push(p);
      byModule.set(p.module, list);
    }
    return [...byModule.entries()].map(([module, items]) => ({ module, items }));
  });

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(128)]],
    description: ['', [Validators.maxLength(256)]],
  });

  get f() {
    return this.form.controls;
  }

  ngOnInit(): void {
    const id = this.roleId();
    if (id) {
      this.loading.set(true);
      this.service.getRole(id).subscribe({
        next: (r) => {
          this.isSystem.set(r.isSystem);
          this.selected.set(new Set(r.permissionKeys));
          this.form.patchValue({ name: r.name, description: r.description });
          if (r.isSystem) this.form.disable();
          this.loading.set(false);
          this.dlg().nativeElement.showModal();
        },
        error: (e) => {
          this.error.set(problemDetail(e, 'تعذّر تحميل بيانات الدور.'));
          this.loading.set(false);
          this.dlg().nativeElement.showModal();
        },
      });
    } else {
      this.dlg().nativeElement.showModal();
    }
  }

  togglePerm(key: string): void {
    if (this.isSystem()) return;
    this.selected.update((s) => {
      const next = new Set(s);
      if (next.has(key)) next.delete(key);
      else next.add(key);
      return next;
    });
  }

  isPermSelected(key: string): boolean {
    return this.selected().has(key);
  }

  toggleModule(mod: PermModule): void {
    if (this.isSystem()) return;
    const allOn = mod.items.every((p) => this.selected().has(p.key));
    this.selected.update((s) => {
      const next = new Set(s);
      for (const p of mod.items) {
        if (allOn) next.delete(p.key);
        else next.add(p.key);
      }
      return next;
    });
  }

  moduleState(mod: PermModule): 'all' | 'some' | 'none' {
    const on = mod.items.filter((p) => this.selected().has(p.key)).length;
    if (on === 0) return 'none';
    if (on === mod.items.length) return 'all';
    return 'some';
  }

  submit(): void {
    if (this.isSystem()) {
      this.cancel();
      return;
    }
    this.error.set(null);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.saving.set(true);
    const raw = this.form.getRawValue();
    const permissionKeys = [...this.selected()];

    if (this.isNew()) {
      this.service
        .createRole({
          name: raw.name,
          description: raw.description,
          permissionKeys,
        })
        .subscribe({
          next: () => this.finish(),
          error: (e) => this.fail(e, 'تعذّر إنشاء الدور.'),
        });
    } else {
      const id = this.roleId()!;
      this.service
        .updateRole(id, { name: raw.name, description: raw.description })
        .subscribe({
          next: () =>
            this.service.setRolePermissions(id, permissionKeys).subscribe({
              next: () => this.finish(),
              error: (e) =>
                this.fail(e, 'حُفظت البيانات لكن تعذّر حفظ الصلاحيات.'),
            }),
          error: (e) => this.fail(e, 'تعذّر حفظ بيانات الدور.'),
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
