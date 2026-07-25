import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { ClaimsService } from '../../../core/claims/claims.service';
import { ClaimSummary } from '../../../core/claims/claim.models';
import { claimStatusLabel } from '../../../shared/status-labels';

@Component({
  selector: 'app-dashboard',
  imports: [],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard {
  private readonly auth = inject(AuthService);
  private readonly claimsService = inject(ClaimsService);
  private readonly router = inject(Router);

  protected readonly user = this.auth.currentUser;
  protected readonly claims = signal<ClaimSummary[]>([]);
  protected readonly loading = signal(true);

  constructor() {
    this.claimsService.getMyClaims().subscribe({
      next: (list) => {
        this.claims.set(list);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  protected firstName(): string {
    return this.user()?.fullName.split(' ')[0] ?? '';
  }

  protected statusLabel(status: string): string {
    return claimStatusLabel(status);
  }

  protected badgeClass(status: string): string {
    if (status === 'AutoApproved' || status === 'OperatorApproved') return 'badge--approved';
    if (status === 'AutoRejected' || status === 'OperatorRejected') return 'badge--rejected';
    if (status === 'Submitted' || status === 'Processing') return 'badge--processing';
    return 'badge--neutral';
  }

  protected startClaim(): void {
    this.router.navigate(['/kupac/nova']);
  }

  protected openClaim(id: number): void {
    this.router.navigate(['/kupac/reklamacija', id]);
  }
}
