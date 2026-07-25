import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ClaimsService } from '../../../core/claims/claims.service';
import { PurchaseType } from '../../../core/claims/claim.models';

const STEP_LABELS = ['Kupovina', 'Fotografije', 'Opis', 'Pregled'];

@Component({
  selector: 'app-claim-wizard',
  imports: [ReactiveFormsModule],
  templateUrl: './claim-wizard.html',
  styleUrl: './claim-wizard.scss',
})
export class ClaimWizard {
  private readonly fb = inject(FormBuilder);
  private readonly claims = inject(ClaimsService);
  private readonly router = inject(Router);

  protected readonly steps = STEP_LABELS;
  protected readonly step = signal(1);
  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly purchaseType = signal<PurchaseType>('InStore');
  protected readonly receiptFileName = signal<string>('');
  protected readonly photoFileNames = signal<string[]>([]);

  protected readonly form = this.fb.nonNullable.group({
    documentNumber: [''],
    issueType: ['Oštećenje pri dostavi', [Validators.required]],
    issueDescription: ['', [Validators.required, Validators.minLength(10)]],
  });

  protected readonly isLastStep = computed(() => this.step() === STEP_LABELS.length);

  protected readonly canGoNext = computed(() => {
    switch (this.step()) {
      case 1:
        return this.purchaseType() === 'InStore'
          ? this.receiptFileName().length > 0
          : this.form.controls.documentNumber.value.trim().length > 0;
      case 2:
        return this.photoFileNames().length > 0;
      case 3:
        return this.form.controls.issueDescription.valid;
      default:
        return true;
    }
  });

  protected setPurchaseType(type: PurchaseType): void {
    this.purchaseType.set(type);
  }

  protected onReceiptSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    this.receiptFileName.set(file ? file.name : '');
  }

  protected onPhotosSelected(event: Event): void {
    const files = Array.from((event.target as HTMLInputElement).files ?? []);
    const names = files.map((f) => f.name);
    this.photoFileNames.set([...this.photoFileNames(), ...names].slice(0, 5));
  }

  protected removePhoto(name: string): void {
    this.photoFileNames.set(this.photoFileNames().filter((n) => n !== name));
  }

  protected back(): void {
    if (this.step() > 1) {
      this.step.set(this.step() - 1);
    }
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
        receiptFileName: this.receiptFileName(),
        photoFileNames: this.photoFileNames(),
        issueType: raw.issueType,
        issueDescription: raw.issueDescription,
      })
      .subscribe({
        next: (result) =>
          this.router.navigate(['/kupac/reklamacija', result.id, 'potvrda']),
        error: () => {
          this.submitting.set(false);
          this.errorMessage.set('Slanje nije uspjelo. Pokušajte ponovo.');
        },
      });
  }
}
