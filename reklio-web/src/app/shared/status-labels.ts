// T3.5 — jedno mjesto za prevod statusa reklamacije (engleski enum -> bosanski prikaz).
// Vrijednosti moraju odgovarati backend ClaimStatus enumu.

export type ClaimStatus =
  | 'Submitted'
  | 'Processing'
  | 'AutoApproved'
  | 'AutoRejected'
  | 'Escalated'
  | 'OperatorApproved'
  | 'OperatorRejected';

export const CLAIM_STATUS_LABELS: Record<ClaimStatus, string> = {
  Submitted: 'Zaprimljeno',
  Processing: 'U obradi',
  AutoApproved: 'Automatski odobreno',
  AutoRejected: 'Automatski odbijeno',
  Escalated: 'Proslijeđeno operateru',
  OperatorApproved: 'Odobreno (operater)',
  OperatorRejected: 'Odbijeno (operater)',
};

export function claimStatusLabel(status: string): string {
  return CLAIM_STATUS_LABELS[status as ClaimStatus] ?? status;
}