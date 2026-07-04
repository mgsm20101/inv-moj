import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import {
  FormControl,
  ReactiveFormsModule,
} from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { CustodyService } from '../custody.service';
import {
  CUSTODY_ITEM_STATUS_BADGE,
  CUSTODY_ITEM_STATUS_LABEL,
  CustodyItemStatus,
  CustodyStatementDto,
  EMPLOYEE_STATUS_BADGE,
  EMPLOYEE_STATUS_LABEL,
  EmployeeDto,
  EmployeeStatus,
} from '../../../core/models/custody.models';
import { ConfirmDialogComponent } from '../../../shared/confirm-dialog/confirm-dialog.component';
import { HasPermissionDirective } from '../../../shared/has-permission.directive';

/**
 * كشف العُهد (المرحلة ٣) — «الدفتر الرسمي»:
 * بحث موظف → كشف عهدته (بنود + قيمة إجمالية + حالة) → براءة ذمة.
 * براءة الذمة تفشل بـ 409/Custody.NotEmpty إن بقيت بنود قائمة — تُعرض رسالة الخادم.
 */
@Component({
  selector: 'wims-custody-statement',
  standalone: true,
  imports: [
    DatePipe,
    DecimalPipe,
    RouterLink,
    ReactiveFormsModule,
    ConfirmDialogComponent,
    HasPermissionDirective,
  ],
  templateUrl: './custody-statement.component.html',
  styleUrl: './custody-statement.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CustodyStatementComponent implements OnInit {
  private readonly service = inject(CustodyService);
  private readonly destroyRef = inject(DestroyRef);

  readonly search = new FormControl('', { nonNullable: true });

  readonly employees = signal<EmployeeDto[]>([]);
  readonly searching = signal(false);
  readonly selected = signal<EmployeeDto | null>(null);

  readonly statement = signal<CustodyStatementDto | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly includeHistory = signal(false);

  readonly clearing = signal(false);
  readonly clearError = signal<string | null>(null);
  readonly cleared = signal(false);

  /** معرّف بند العهدة الجاري إرجاعه حالياً (null = لا شيء قيد التنفيذ). */
  readonly returningId = signal<string | null>(null);
  readonly returnError = signal<string | null>(null);

  readonly empStatusLabel = EMPLOYEE_STATUS_LABEL;
  readonly empStatusBadge = EMPLOYEE_STATUS_BADGE;
  readonly itemStatusLabel = CUSTODY_ITEM_STATUS_LABEL;
  readonly itemStatusBadge = CUSTODY_ITEM_STATUS_BADGE;

  private readonly clearDlg =
    viewChild.required<ConfirmDialogComponent>('clearDlg');

  /** عدد البنود القائمة فعلاً (في العهدة) — يحكم إتاحة براءة الذمة. */
  readonly openItemCount = computed(
    () =>
      (this.statement()?.items ?? []).filter(
        (i) => i.status === CustodyItemStatus.InCustody,
      ).length,
  );

  readonly canClear = computed(() => {
    const s = this.statement();
    return !!s && this.openItemCount() === 0 && !this.cleared();
  });

  ngOnInit(): void {
    this.search.valueChanges
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((term) => this.runSearch(term));
    // تحميل أولي بأول دفعة موظفين.
    this.runSearch('');
  }

  private runSearch(term: string): void {
    this.searching.set(true);
    this.service.employees(term).subscribe({
      next: (list) => {
        this.employees.set(list);
        this.searching.set(false);
      },
      error: () => {
        this.employees.set([]);
        this.searching.set(false);
      },
    });
  }

  select(emp: EmployeeDto): void {
    this.selected.set(emp);
    this.cleared.set(false);
    this.clearError.set(null);
    this.loadStatement();
  }

  toggleHistory(): void {
    this.includeHistory.update((v) => !v);
    if (this.selected()) this.loadStatement();
  }

  private loadStatement(): void {
    const emp = this.selected();
    if (!emp) return;
    this.loading.set(true);
    this.error.set(null);
    this.service.statement(emp.id, this.includeHistory()).subscribe({
      next: (s) => {
        this.statement.set(s);
        this.loading.set(false);
      },
      error: (err: { status?: number }) => {
        this.error.set(
          err?.status === 404
            ? 'لا يوجد سجل عهدة لهذا الموظف.'
            : 'تعذّر تحميل كشف العهدة.',
        );
        this.statement.set(null);
        this.loading.set(false);
      },
    });
  }

  openClear(): void {
    this.clearDlg().open();
  }

  onClear(): void {
    const emp = this.selected();
    if (!emp) return;
    this.clearing.set(true);
    this.clearError.set(null);
    this.service.clear(emp.id).subscribe({
      next: () => {
        this.clearing.set(false);
        this.cleared.set(true);
        this.loadStatement();
      },
      error: (err: { error?: { detail?: string; message?: string }; status?: number }) => {
        this.clearing.set(false);
        // رسالة الخادم (ProblemDetails.detail) — مثل «يوجد N بند عهدة قائم…».
        this.clearError.set(
          err?.error?.detail ??
            err?.error?.message ??
            (err?.status === 403
              ? 'لا تملك صلاحية براءة الذمة.'
              : 'تعذّرت براءة الذمة. أعِد المحاولة.'),
        );
      },
    });
  }

  returnItem(custodyItemId: string): void {
    this.returningId.set(custodyItemId);
    this.returnError.set(null);
    this.service.returnItem(custodyItemId).subscribe({
      next: () => {
        this.returningId.set(null);
        this.loadStatement();
      },
      error: (err: { error?: { detail?: string; message?: string }; status?: number }) => {
        this.returningId.set(null);
        this.returnError.set(
          err?.error?.detail ??
            err?.error?.message ??
            (err?.status === 403
              ? 'لا تملك صلاحية إرجاع بنود العهدة.'
              : 'تعذّر إرجاع البند. أعِد المحاولة.'),
        );
      },
    });
  }

  readonly CustodyItemStatus = CustodyItemStatus;
  readonly EmployeeStatus = EmployeeStatus;
}
