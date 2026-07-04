import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  output,
} from '@angular/core';

/** ترقيم صفحات هادئ للجداول الكثيفة (RTL). */
@Component({
  selector: 'wims-paginator',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="pager">
      <span class="pager__info num">
        {{ from() }}–{{ to() }} من {{ total() }}
      </span>
      <div class="pager__ctrls">
        <button
          class="btn btn--secondary pager__btn"
          (click)="go(page() - 1)"
          [disabled]="page() <= 1"
          aria-label="الصفحة السابقة"
        >
          <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M15 6l6 6-6 6" /></svg>
        </button>
        <span class="pager__page num">صفحة {{ page() }} / {{ pages() }}</span>
        <button
          class="btn btn--secondary pager__btn"
          (click)="go(page() + 1)"
          [disabled]="page() >= pages()"
          aria-label="الصفحة التالية"
        >
          <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M9 6l-6 6 6 6" /></svg>
        </button>
      </div>
    </div>
  `,
  styles: [
    `
      .pager {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: var(--space-md);
        padding: var(--space-sm) var(--space-md);
        flex-wrap: wrap;
      }
      .pager__info { font-size: var(--fs-small); color: var(--text-muted); }
      .pager__ctrls { display: flex; align-items: center; gap: var(--space-sm); }
      .pager__page { font-size: var(--fs-small); color: var(--ink); min-width: 12ch; text-align: center; }
      .pager__btn {
        min-height: 34px;
        width: 34px;
        padding: 0;
        svg {
          width: 18px; height: 18px; fill: none; stroke: currentColor;
          stroke-width: 1.8; stroke-linecap: round; stroke-linejoin: round;
        }
      }
    `,
  ],
})
export class PaginatorComponent {
  readonly page = input(1);
  readonly pageSize = input(20);
  readonly total = input(0);

  readonly pageChange = output<number>();

  readonly pages = computed(() =>
    Math.max(1, Math.ceil(this.total() / this.pageSize())),
  );
  readonly from = computed(() =>
    this.total() === 0 ? 0 : (this.page() - 1) * this.pageSize() + 1,
  );
  readonly to = computed(() =>
    Math.min(this.page() * this.pageSize(), this.total()),
  );

  go(p: number): void {
    if (p >= 1 && p <= this.pages() && p !== this.page()) {
      this.pageChange.emit(p);
    }
  }
}
