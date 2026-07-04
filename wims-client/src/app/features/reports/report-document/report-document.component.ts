import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { DatePipe } from '@angular/common';
import {
  ReportAlignment,
  ReportDocument,
} from '../../../core/models/reports.models';

/**
 * عارض مستند تقرير عام (المرحلة ٥) — يعرض أيّ ReportDocument:
 * عنوان + بيانات وصفية + جدول (أعمدة بمحاذاتها، صفوف نصية مُنسّقة، إجمالي).
 * الخلايا نصوص جاهزة من الخادم؛ لا تنسيق هنا.
 */
@Component({
  selector: 'wims-report-document',
  standalone: true,
  imports: [DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (doc(); as d) {
      <article class="rdoc">
        <header class="rdoc__head">
          <div>
            <h2 class="rdoc__title">{{ d.title }}</h2>
            @if (d.subtitle) { <p class="rdoc__subtitle">{{ d.subtitle }}</p> }
          </div>
          <p class="rdoc__gen num">صدر: {{ d.generatedAt | date: 'yyyy/MM/dd HH:mm' }}</p>
        </header>

        @if (d.meta.length) {
          <dl class="rdoc__meta">
            @for (m of d.meta; track m.label) {
              <div><dt>{{ m.label }}</dt><dd class="num">{{ m.value }}</dd></div>
            }
          </dl>
        }

        <div class="rdoc__table-wrap">
          <table class="table rdoc__table">
            <thead>
              <tr>
                @for (c of d.columns; track $index) {
                  <th [style.text-align]="alignOf(c.align)">{{ c.header }}</th>
                }
              </tr>
            </thead>
            <tbody>
              @for (row of d.rows; track $index) {
                <tr>
                  @for (cell of row; track $index) {
                    <td class="num" [style.text-align]="alignAt(d, $index)">{{ cell }}</td>
                  }
                </tr>
              }
            </tbody>
            @if (d.totals) {
              <tfoot>
                <tr>
                  @for (t of d.totals; track $index) {
                    <td class="num rdoc__total" [style.text-align]="alignAt(d, $index)">{{ t }}</td>
                  }
                </tr>
              </tfoot>
            }
          </table>

          @if (d.rows.length === 0) {
            <p class="rdoc__empty">لا بيانات مطابقة لهذا التقرير.</p>
          }
        </div>
      </article>
    }
  `,
  styles: [
    `
      .rdoc { display: block; }
      .rdoc__head {
        display: flex; align-items: flex-start; justify-content: space-between;
        gap: var(--space-md); flex-wrap: wrap; margin-bottom: var(--space-md);
      }
      .rdoc__title { font-size: var(--fs-h1); font-family: var(--font-doc); }
      .rdoc__subtitle { margin-top: 2px; color: var(--text-muted); font-size: var(--fs-body); }
      .rdoc__gen { color: var(--text-muted); font-size: var(--fs-small); }
      .rdoc__meta {
        display: grid; grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
        gap: var(--space-md); margin: 0 0 var(--space-md);
        padding: var(--space-md); background: var(--paper); border-radius: var(--radius-md);
      }
      .rdoc__meta div { display: flex; flex-direction: column; gap: 2px; }
      .rdoc__meta dt { font-size: var(--fs-small); color: var(--text-muted); margin: 0; }
      .rdoc__meta dd { margin: 0; color: var(--ink); font-size: var(--fs-body); font-weight: var(--fw-medium); }
      .rdoc__table-wrap { overflow-x: auto; }
      .rdoc__table { min-width: 640px; }
      .rdoc__table tfoot td {
        border-top: 2px solid var(--border-strong); border-bottom: none;
        padding-top: var(--space-sm); font-weight: var(--fw-semibold); color: var(--ink);
      }
      .rdoc__empty { padding: var(--space-xl); text-align: center; color: var(--text-muted); }
    `,
  ],
})
export class ReportDocumentComponent {
  readonly doc = input.required<ReportDocument>();

  /** يترجم محاذاة الخادم إلى قيمة CSS. */
  alignOf(a: ReportAlignment | undefined): string {
    switch (a) {
      case ReportAlignment.Center:
        return 'center';
      case ReportAlignment.Left:
        return 'left';
      default:
        return 'right';
    }
  }

  /** محاذاة خلية حسب فهرس عمودها (يتحمّل نقص التطابق بين الخلايا والأعمدة). */
  alignAt(d: ReportDocument, index: number): string {
    return this.alignOf(d.columns[index]?.align);
  }
}
