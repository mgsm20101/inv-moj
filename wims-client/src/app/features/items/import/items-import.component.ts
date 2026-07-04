import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  signal,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { ItemsService } from '../items.service';
import { ImportResult } from '../../../core/models/catalog.models';

type Phase = 'idle' | 'previewing' | 'previewed' | 'committing' | 'committed';

const MAX_BYTES = 20_000_000;
const ACCEPT = ['.xlsx', '.xls'];

/**
 * استيراد الأصناف من Excel — تدفّق آمن على خطوتين:
 * 1) معاينة (commit=false): تحقق وتقرير أخطاء بلا حفظ.
 * 2) اعتماد (commit=true): حفظ ذرّي، متاح فقط لو لا أخطاء.
 */
@Component({
  selector: 'wims-items-import',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './items-import.component.html',
  styleUrl: './items-import.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ItemsImportComponent {
  private readonly service = inject(ItemsService);

  readonly phase = signal<Phase>('idle');
  readonly file = signal<File | null>(null);
  readonly result = signal<ImportResult | null>(null);
  readonly error = signal<string | null>(null);
  readonly dragOver = signal(false);

  readonly canCommit = computed(() => {
    const r = this.result();
    return this.phase() === 'previewed' && !!r && !r.hasErrors && r.validRows > 0;
  });

  onDragOver(e: DragEvent): void {
    e.preventDefault();
    this.dragOver.set(true);
  }
  onDragLeave(): void {
    this.dragOver.set(false);
  }
  onDrop(e: DragEvent): void {
    e.preventDefault();
    this.dragOver.set(false);
    const f = e.dataTransfer?.files?.[0];
    if (f) this.select(f);
  }
  onPick(e: Event): void {
    const f = (e.target as HTMLInputElement).files?.[0];
    if (f) this.select(f);
  }

  private select(f: File): void {
    this.error.set(null);
    this.result.set(null);
    const ext = f.name.slice(f.name.lastIndexOf('.')).toLowerCase();
    if (!ACCEPT.includes(ext)) {
      this.error.set('صيغة غير مدعومة. المطلوب ملف Excel بصيغة ‎.xlsx‎ أو ‎.xls‎.');
      return;
    }
    if (f.size > MAX_BYTES) {
      this.error.set('حجم الملف يتجاوز الحد المسموح (20 ميجابايت).');
      return;
    }
    this.file.set(f);
    this.phase.set('idle');
    this.preview();
  }

  preview(): void {
    const f = this.file();
    if (!f) return;
    this.phase.set('previewing');
    this.error.set(null);
    this.service.importItems(f, false).subscribe({
      next: (r) => {
        this.result.set(r);
        this.phase.set('previewed');
      },
      error: (err) => {
        this.error.set(this.readError(err));
        this.phase.set('idle');
      },
    });
  }

  commit(): void {
    const f = this.file();
    if (!f || !this.canCommit()) return;
    this.phase.set('committing');
    this.error.set(null);
    this.service.importItems(f, true).subscribe({
      next: (r) => {
        this.result.set(r);
        this.phase.set('committed');
      },
      error: (err) => {
        this.error.set(this.readError(err));
        this.phase.set('previewed');
      },
    });
  }

  reset(): void {
    this.file.set(null);
    this.result.set(null);
    this.error.set(null);
    this.phase.set('idle');
  }

  /** رقم الصف في العرض: الـ API صفري + صف العنوان → +2. */
  displayRow(rowNumber: number): number {
    return rowNumber + 2;
  }

  private readError(err: unknown): string {
    const e = err as { error?: { message?: string }; status?: number };
    if (e?.error?.message) return e.error.message;
    if (e?.status === 401) return 'الجلسة منتهية. سجّل الدخول من جديد.';
    if (e?.status === 403) return 'لا تملك صلاحية تنفيذ الاستيراد (Import.Execute).';
    return 'تعذّر معالجة الملف. راجع الاتصال بالخادم ثم أعِد المحاولة.';
  }
}
