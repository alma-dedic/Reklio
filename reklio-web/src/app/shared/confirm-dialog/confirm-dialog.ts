import { Component, HostListener, input, output } from '@angular/core';

// Jednostavan, reusable dijalog za potvrdu (zamjena za browser confirm()).
// Roditelj ga uslovno renderuje i sluša (confirmed)/(cancelled).
@Component({
  selector: 'app-confirm-dialog',
  imports: [],
  templateUrl: './confirm-dialog.html',
  styleUrl: './confirm-dialog.scss',
})
export class ConfirmDialog {
  readonly title = input.required<string>();
  readonly message = input('');
  readonly confirmText = input('Potvrdi');
  readonly cancelText = input('Odustani');
  readonly danger = input(false);

  readonly confirmed = output<void>();
  readonly cancelled = output<void>();

  @HostListener('document:keydown.escape')
  protected onEscape(): void {
    this.cancelled.emit();
  }
}
