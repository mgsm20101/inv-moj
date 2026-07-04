import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  input,
  output,
  viewChild,
} from '@angular/core';

/**
 * حوار تأكيد بعنصر <dialog> الأصلي (يهرب من stacking context / overflow).
 * يُفتح عبر open() ويصدر confirmed/cancelled.
 */
@Component({
  selector: 'wims-confirm-dialog',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <dialog #dlg class="cd" (cancel)="onCancel($event)">
      <h3 class="cd__title">{{ title() }}</h3>
      <p class="cd__msg">{{ message() }}</p>
      @if (withReason()) {
        <textarea
          #reason
          class="input cd__reason"
          rows="3"
          [placeholder]="reasonPlaceholder()"
        ></textarea>
      }
      <div class="cd__actions">
        <button class="btn btn--secondary" (click)="cancel()">{{ cancelLabel() }}</button>
        <button
          [class]="'btn ' + (danger() ? 'btn--danger' : 'btn--justice')"
          (click)="confirm()"
        >
          {{ confirmLabel() }}
        </button>
      </div>
    </dialog>
  `,
  styles: [
    `
      .cd {
        border: none;
        border-radius: var(--radius-lg);
        padding: var(--space-lg);
        max-width: 440px;
        width: calc(100% - 2 * var(--space-lg));
        box-shadow: var(--shadow-pop);
        color: var(--text);
      }
      .cd::backdrop { background: rgba(23, 38, 59, 0.42); }
      .cd__title { font-size: var(--fs-h1); margin-bottom: var(--space-sm); }
      .cd__msg { color: var(--text-muted); }
      .cd__reason { width: 100%; margin-top: var(--space-md); resize: vertical; padding: var(--space-sm) var(--space-md); }
      .cd__actions { display: flex; justify-content: flex-end; gap: var(--space-sm); margin-top: var(--space-lg); }
    `,
  ],
})
export class ConfirmDialogComponent {
  readonly title = input('تأكيد');
  readonly message = input('');
  readonly confirmLabel = input('تأكيد');
  readonly cancelLabel = input('إلغاء');
  readonly danger = input(false);
  readonly withReason = input(false);
  readonly reasonPlaceholder = input('السبب…');

  readonly confirmed = output<string | undefined>();
  readonly cancelled = output<void>();

  private readonly dlg = viewChild.required<ElementRef<HTMLDialogElement>>('dlg');
  private readonly reason =
    viewChild<ElementRef<HTMLTextAreaElement>>('reason');

  open(): void {
    this.dlg().nativeElement.showModal();
  }

  confirm(): void {
    const reasonText = this.reason()?.nativeElement.value?.trim() || undefined;
    this.dlg().nativeElement.close();
    this.confirmed.emit(reasonText);
  }

  cancel(): void {
    this.dlg().nativeElement.close();
    this.cancelled.emit();
  }

  onCancel(e: Event): void {
    e.preventDefault();
    this.cancel();
  }
}
