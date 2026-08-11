import { Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ClaimsService } from '../../../core/claims/claims.service';
import { PurchaseType } from '../../../core/claims/claim.models';
import { ConfirmDialog } from '../../../shared/confirm-dialog/confirm-dialog';

const STEP_LABELS = ['Kupovina', 'Fotografije', 'Opis', 'Pregled'];
const MAX_PHOTOS = 3;

@Component({
  selector: 'app-claim-wizard',
  imports: [ReactiveFormsModule, ConfirmDialog],
  templateUrl: './claim-wizard.html',
  styleUrl: './claim-wizard.scss',
})
export class ClaimWizard {
  private readonly fb = inject(FormBuilder);
  private readonly claims = inject(ClaimsService);
  private readonly router = inject(Router);

  protected readonly steps = STEP_LABELS;
  protected readonly maxPhotos = MAX_PHOTOS;
  protected readonly step = signal(1);
  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly showCancelConfirm = signal(false);

  protected readonly purchaseType = signal<PurchaseType>('InStore');
  protected readonly receiptFile = signal<File | null>(null);
  protected readonly photoFiles = signal<File[]>([]);

  protected readonly form = this.fb.nonNullable.group({
    documentNumber: [''],
    issueType: ['Oštećenje pri dostavi', [Validators.required]],
    issueDescription: ['', [Validators.required, Validators.minLength(10)]],
  });

  // Vrijednost forme kao signal — bez ovoga se canGoNext ne preračunava dok kucaš
  // (FormControl.valid/.value nisu signali). valueChanges okida na svaku promjenu.
  private readonly formValue = toSignal(this.form.valueChanges, {
    initialValue: this.form.getRawValue(),
  });

  protected readonly isLastStep = computed(() => this.step() === STEP_LABELS.length);

  protected readonly canGoNext = computed(() => {
    const value = this.formValue();
    switch (this.step()) {
      case 1:
        return this.purchaseType() === 'InStore'
          ? this.receiptFile() !== null
          : (value.documentNumber ?? '').trim().length > 0;
      case 2:
        return this.photoFiles().length > 0;
      case 3:
        return (value.issueDescription ?? '').trim().length >= 10;
      default:
        return true;
    }
  });

  protected setPurchaseType(type: PurchaseType): void {
    this.purchaseType.set(type);
  }

  protected onReceiptSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0] ?? null;
    this.receiptFile.set(file);
  }

  protected onPhotosSelected(event: Event): void {
    const files = Array.from((event.target as HTMLInputElement).files ?? []);
    this.photoFiles.set([...this.photoFiles(), ...files].slice(0, MAX_PHOTOS));
  }

  protected removePhoto(index: number): void {
    this.photoFiles.set(this.photoFiles().filter((_, i) => i !== index));
  }

  protected back(): void {
    if (this.step() > 1) {
      this.step.set(this.step() - 1);
    }
  }

  protected cancel(): void {
    const raw = this.form.getRawValue();
    const hasData =
      this.receiptFile() !== null ||
      this.photoFiles().length > 0 ||
      raw.documentNumber.trim().length > 0 ||
      raw.issueDescription.trim().length > 0;

    if (hasData) {
      this.showCancelConfirm.set(true);
    } else {
      this.router.navigate(['/kupac']);
    }
  }

  protected confirmCancel(): void {
    this.showCancelConfirm.set(false);
    this.router.navigate(['/kupac']);
  }

  protected next(): void {
    if (!this.canGoNext()) {
      this.form.markAllAsTouched();
      return;
    }

    if (!this.isLastStep()) {
      this.step.set(this.step() + 1);
      return;
    }

    this.submit();
  }

  private submit(): void {
    this.submitting.set(true);
    this.errorMessage.set(null);

    const raw = this.form.getRawValue();

    this.claims
      .submitClaim({
        purchaseType: this.purchaseType(),
        documentNumber: raw.documentNumber,
        receiptFile: this.receiptFile(),
        photoFiles: this.photoFiles(),
        issueType: raw.issueType,
        issueDescription: raw.issueDescription,
      })
      .subscribe({
        next: (result) =>
          this.router.navigate(['/kupac/reklamacija', result.id, 'potvrda']),
        error: (err) => {
          this.submitting.set(false);
          this.errorMessage.set(
            err?.error?.message ?? 'Slanje nije uspjelo. Pokušajte ponovo.',
          );
        },
      });
  }
}
