import { Component, HostListener, input, linkedSignal, output } from '@angular/core';
import { FormsModule } from '@angular/forms';

// Jednostavan, reusable dijalog za potvrdu (zamjena za browser confirm()).
// Opcionalno polje za razlog (npr. operaterovo odbijanje) — confirmed emituje taj tekst.
@Component({
  selector: 'app-confirm-dialog',
  imports: [FormsModule],
  templateUrl: './confirm-dialog.html',
  styleUrl: './confirm-dialog.scss',
})
export class ConfirmDialog {
  readonly title = input.required<string>();
  readonly message = input('');
  readonly confirmText = input('Potvrdi');
  readonly cancelText = input('Odustani');
  readonly danger = input(false);
  readonly showReason = input(false);
  readonly reasonLabel = input('Razlog');
  readonly reasonRequired = input(false);
  readonly initialReason = input('');

  // Krene od prefill-a (npr. AI draft razloga), ali operater može prepisati.
  protected readonly reason = linkedSignal(() => this.initialReason());

  readonly confirmed = output<string>();
  readonly cancelled = output<void>();

  protected canConfirm(): boolean {
    return !this.reasonRequired() || this.reason().trim().length > 0;
  }

  protected onConfirm(): void {
    if (this.canConfirm()) {
      this.confirmed.emit(this.reason().trim());
    }
  }

  @HostListener('document:keydown.escape')
  protected onEscape(): void {
    this.cancelled.emit();
  }
}
