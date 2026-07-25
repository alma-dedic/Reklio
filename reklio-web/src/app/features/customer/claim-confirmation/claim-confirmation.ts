import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ClaimsService } from '../../../core/claims/claims.service';

@Component({
  selector: 'app-claim-confirmation',
  imports: [],
  templateUrl: './claim-confirmation.html',
  styleUrl: './claim-confirmation.scss',
})
export class ClaimConfirmation {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly claims = inject(ClaimsService);

  protected readonly claimId = Number(this.route.snapshot.paramMap.get('id'));
  protected readonly reference = signal('');

  constructor() {
    this.claims.getClaim(this.claimId).subscribe({
      next: (claim) => this.reference.set(claim.reference),
    });
  }

  protected trackStatus(): void {
    this.router.navigate(['/kupac/reklamacija', this.claimId]);
  }

  protected goHome(): void {
    this.router.navigate(['/kupac']);
  }
}
