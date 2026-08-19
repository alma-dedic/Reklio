import { Component, OnDestroy, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, switchMap, take, takeUntil, takeWhile, timer } from 'rxjs';
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
  protected readonly steps = ['Poslano', 'AI analiza', 'Pregled', 'Odluka'];

  constructor() {
    const id = Number(this.route.snapshot.paramMap.get('id'));

    // T3.4 — polling dok je reklamacija u obradi; prekida se na ngOnDestroy.
    // Nastavljamo dok status nije konačan I dok AI nalaz nije upisan — status se
    // snima par sekundi prije AnalysisJson snapshot-a, pa bez ovoga ekran zamrzne
    // na "Čeka se...". take(60) je sigurnosni limit (~3 min).
    timer(0, 3000)
      .pipe(
        take(60),
        switchMap(() => this.claims.getClaim(id)),
        takeWhile(
          (claim) =>
            claim.status === 'Processing' ||
            claim.status === 'Submitted' ||
            claim.analysis === null,
          true,
        ),
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

  protected isRejected(): boolean {
    const s = this.claim()?.status;
    return s === 'AutoRejected' || s === 'OperatorRejected';
  }

  // Stanje koraka (1-bazirano) iz stvarnog statusa reklamacije.
  protected stepState(step: number): 'done' | 'active' | 'pending' {
    const s = this.claim()?.status;
    if (!s) return 'pending';
    const isFinal =
      s === 'AutoApproved' ||
      s === 'AutoRejected' ||
      s === 'OperatorApproved' ||
      s === 'OperatorRejected';

    switch (step) {
      case 1: // Poslano
        return 'done';
      case 2: // AI analiza
        if (s === 'Submitted') return 'pending';
        if (s === 'Processing') return 'active';
        return 'done';
      case 3: // Pregled
        if (isFinal) return 'done';
        if (s === 'Escalated') return 'active';
        return 'pending';
      case 4: // Odluka
        return isFinal ? 'done' : 'pending';
      default:
        return 'pending';
    }
  }

  protected goBack(): void {
    this.router.navigate(['/kupac']);
  }
}
