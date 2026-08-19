import { Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ClaimsService } from '../../../core/claims/claims.service';
import {
  PurchaseType,
  ResolveProduct,
  ResolveReceiptResult,
} from '../../../core/claims/claim.models';
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

  // Razrješavanje računa → proizvodi za dropdown.
  protected readonly resolving = signal(false);
  protected readonly resolveResult = signal<ResolveReceiptResult | null>(null);
  protected readonly selectedPurchaseId = signal<number | null>(null);

  protected readonly form = this.fb.nonNullable.group({
    documentNumber: [''],
    issueType: ['Fizičko oštećenje', [Validators.required]],
    issueDescription: ['', [Validators.required, Validators.minLength(10)]],
  });

  // Vrijednost forme kao signal — bez ovoga se canGoNext ne preračunava dok kucaš
  // (FormControl.valid/.value nisu signali). valueChanges okida na svaku promjenu.
  private readonly formValue = toSignal(this.form.valueChanges, {
    initialValue: this.form.getRawValue(),
  });

  protected readonly isLastStep = computed(() => this.step() === STEP_LABELS.length);

  // Ima dovoljno unosa da se pokrene provjera računa (slika ili broj).
  protected readonly canResolve = computed(() => {
    const value = this.formValue();
    return this.purchaseType() === 'InStore'
      ? this.receiptFile() !== null
      : (value.documentNumber ?? '').trim().length > 0;
  });

  // Izabrana stavka (za pregled).
  protected readonly selectedProduct = computed<ResolveProduct | null>(() => {
    const id = this.selectedPurchaseId();
    const products = this.resolveResult()?.products ?? [];
    return products.find((p) => p.purchaseId === id) ?? null;
  });

  protected readonly canGoNext = computed(() => {
    const value = this.formValue();
    switch (this.step()) {
      case 1:
        // Mora biti izabran proizvod (pronađena kupovina). Bez kupovine → ne može dalje.
        return this.selectedPurchaseId() !== null;
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
    this.resetResolve();
  }

  protected onReceiptSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0] ?? null;
    this.receiptFile.set(file);
    this.resetResolve();
    // Čim je slika izabrana → automatski provjeri račun (bez posebnog dugmeta).
    if (file) {
      this.resolve();
    }
  }

  protected onPhotosSelected(event: Event): void {
    const files = Array.from((event.target as HTMLInputElement).files ?? []);
    this.photoFiles.set([...this.photoFiles(), ...files].slice(0, MAX_PHOTOS));
  }

  protected removePhoto(index: number): void {
    this.photoFiles.set(this.photoFiles().filter((_, i) => i !== index));
  }

  // Reset kad se promijeni izvor računa (slika/broj/tip) — traži novu provjeru.
  protected resetResolve(): void {
    this.resolveResult.set(null);
    this.selectedPurchaseId.set(null);
  }

  protected resolve(): void {
    if (!this.canResolve() || this.resolving()) {
      return;
    }
    this.resolving.set(true);
    this.errorMessage.set(null);

    const source$ =
      this.purchaseType() === 'InStore'
        ? this.claims.resolveReceipt(this.receiptFile()!)
        : this.claims.resolvePurchase((this.form.getRawValue().documentNumber ?? '').trim());

    source$.subscribe({
      next: (result) => {
        this.resolveResult.set(result);
        this.resolving.set(false);
        // Jedan proizvod → automatski izabran.
        if (result.status === 'ok' && result.products.length === 1) {
          this.selectedPurchaseId.set(result.products[0].purchaseId);
        }
      },
      error: () => {
        this.resolving.set(false);
        this.errorMessage.set('Provjera računa nije uspjela. Pokušajte ponovo.');
      },
    });
  }

  protected onProductSelected(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    this.selectedPurchaseId.set(value ? Number(value) : null);
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
        purchaseId: this.selectedPurchaseId(),
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
