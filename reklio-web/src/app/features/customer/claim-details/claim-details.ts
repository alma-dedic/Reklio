import { Component, OnDestroy, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, switchMap, takeUntil, takeWhile, timer } from 'rxjs';
import { ClaimsService } from '../../../core/claims/claims.service';
import { ClaimDetail } from '../../../core/claims/claim.models';
import { claimStatusLabel } from '../../../shared/status-labels';

@Component({
  selector: 'app-claim-details',
  imports: [],
  templateUrl: './claim-details.html',
  styleUrl: './claim-details.scss',
})
export class ClaimDetails implements OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly claims = inject(ClaimsService);
  private readonly destroy$ = new Subject<void>();

  protected readonly claim = signal<ClaimDetail | null>(null);
  protected readonly loading = signal(true);

  constructor() {
    const id = Number(this.route.snapshot.paramMap.get('id'));

    // T3.4 — polling dok je reklamacija u obradi; prekida se na ngOnDestroy.
    timer(0, 3000)
      .pipe(
        switchMap(() => this.claims.getClaim(id)),
        takeWhile((claim) => claim.status === 'Processing' || claim.status === 'Submitted', true),
        takeUntil(this.destroy$),
      )
      .subscribe({
        next: (claim) => {
          this.claim.set(claim);
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  protected isProcessing(): boolean {
    const status = this.claim()?.status;
    return status === 'Processing' || status === 'Submitted';
  }

  protected statusLabel(): string {
    const status = this.claim()?.status;
    return status ? claimStatusLabel(status) : '';
  }

  protected goBack(): void {
    this.router.navigate(['/kupac']);
  }
}
