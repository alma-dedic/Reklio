import { Component, OnDestroy, inject, signal } from '@angular/core';
import { OperatorService } from '../../../core/operator/operator.service';
import {
  OperatorClaimDetail,
  OperatorClaimSummary,
} from '../../../core/operator/operator.models';
import { ConfirmDialog } from '../../../shared/confirm-dialog/confirm-dialog';

const RISK_HIGH = 0.8684;
const RISK_MID = 0.5;

@Component({
  selector: 'app-operator-panel',
  imports: [ConfirmDialog],
  templateUrl: './operator-panel.html',
  styleUrl: './operator-panel.scss',
})
export class OperatorPanel implements OnDestroy {
  private readonly operator = inject(OperatorService);

  protected readonly claims = signal<OperatorClaimSummary[]>([]);
  protected readonly loadingList = signal(true);
  protected readonly selectedId = signal<number | null>(null);
  protected readonly detail = signal<OperatorClaimDetail | null>(null);
  protected readonly loadingDetail = signal(false);
  protected readonly evidenceUrls = signal<Record<number, string>>({});
  protected readonly processing = signal(false);
  protected readonly confirmAction = signal<'approve' | 'reject' | null>(null);

  constructor() {
    this.loadQueue();
  }

  private loadQueue(): void {
    this.loadingList.set(true);
    this.operator.getEscalated().subscribe({
      next: (list) => {
        this.claims.set(list);
        this.loadingList.set(false);
      },
      error: () => this.loadingList.set(false),
    });
  }

  protected select(id: number): void {
    if (this.selectedId() === id) {
      return;
    }
    this.revokeUrls();
    this.selectedId.set(id);
    this.detail.set(null);
    this.loadingDetail.set(true);
    this.operator.getClaim(id).subscribe({
      next: (d) => {
        this.detail.set(d);
        this.loadingDetail.set(false);
        this.loadEvidence(d);
      },
      error: () => this.loadingDetail.set(false),
    });
  }

  private loadEvidence(d: OperatorClaimDetail): void {
    for (const ev of d.evidence) {
      this.operator.getEvidenceBlob(ev.id).subscribe({
        next: (blob) => {
          const url = URL.createObjectURL(blob);
          this.evidenceUrls.update((m) => ({ ...m, [ev.id]: url }));
        },
        error: () => {},
      });
    }
  }

  protected evidenceUrl(id: number): string | undefined {
    return this.evidenceUrls()[id];
  }

  protected riskLabel(score: number | null): string {
    if (score === null) return '—';
    if (score >= RISK_HIGH) return 'Visok';
    if (score >= RISK_MID) return 'Srednji';
    return 'Nizak';
  }

  protected riskClass(score: number | null | undefined): string {
    if (score === null || score === undefined) return 'op-risk--none';
    if (score >= RISK_HIGH) return 'op-risk--high';
    if (score >= RISK_MID) return 'op-risk--mid';
    return 'op-risk--low';
  }

  protected formatRisk(score: number | null): string {
    if (score === null) return '—';
    return `${score.toFixed(2)} ${score < RISK_HIGH ? '(ispod praga)' : '(iznad praga)'}`;
  }

  protected timeAgo(iso: string): string {
    const min = Math.floor((Date.now() - new Date(iso).getTime()) / 60000);
    if (min < 1) return 'upravo';
    if (min < 60) return `prije ${min} min`;
    const h = Math.floor(min / 60);
    if (h < 24) return `prije ${h} h`;
    return `prije ${Math.floor(h / 24)} d`;
  }

  protected askApprove(): void {
    this.confirmAction.set('approve');
  }

  protected askReject(): void {
    this.confirmAction.set('reject');
  }

  protected confirmDialogTitle(): string {
    return this.confirmAction() === 'approve' ? 'Odobriti reklamaciju?' : 'Odbiti reklamaciju?';
  }

  protected onConfirm(): void {
    const id = this.selectedId();
    const action = this.confirmAction();
    if (id === null || action === null) {
      return;
    }
    this.processing.set(true);
    const req = action === 'approve' ? this.operator.approve(id) : this.operator.reject(id);
    req.subscribe({
      next: () => {
        this.processing.set(false);
        this.confirmAction.set(null);
        this.claims.update((list) => list.filter((c) => c.id !== id));
        this.revokeUrls();
        this.selectedId.set(null);
        this.detail.set(null);
      },
      error: () => {
        this.processing.set(false);
        this.confirmAction.set(null);
      },
    });
  }

  protected cancelConfirm(): void {
    this.confirmAction.set(null);
  }

  private revokeUrls(): void {
    const urls = this.evidenceUrls();
    for (const key of Object.keys(urls)) {
      URL.revokeObjectURL(urls[+key]);
    }
    this.evidenceUrls.set({});
  }

  ngOnDestroy(): void {
    this.revokeUrls();
  }
}
