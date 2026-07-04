import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/** حالة فارغة تُعلّم الواجهة (لا «لا يوجد شيء» فقط). */
@Component({
  selector: 'wims-empty-state',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="empty">
      <span class="empty__icon" aria-hidden="true">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor"
          stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">
          <path d="M4 8l8-4 8 4v8l-8 4-8-4Z" /><path d="M4 8l8 4 8-4M12 12v8" />
        </svg>
      </span>
      <h3 class="empty__title">{{ title() }}</h3>
      @if (message()) { <p class="empty__msg">{{ message() }}</p> }
      <ng-content />
    </div>
  `,
  styles: [
    `
      .empty {
        display: flex;
        flex-direction: column;
        align-items: center;
        text-align: center;
        gap: var(--space-sm);
        padding: var(--space-2xl) var(--space-lg);
        color: var(--text-muted);
      }
      .empty__icon {
        display: grid;
        place-items: center;
        width: 56px;
        height: 56px;
        border-radius: var(--radius-lg);
        background: var(--paper);
        color: var(--border-strong);
        margin-bottom: var(--space-xs);
      }
      .empty__icon svg { width: 30px; height: 30px; }
      .empty__title { font-size: var(--fs-h2); color: var(--ink); }
      .empty__msg { max-width: 42ch; font-size: var(--fs-body); }
    `,
  ],
})
export class EmptyStateComponent {
  readonly title = input('لا توجد بيانات');
  readonly message = input<string | null>(null);
}
