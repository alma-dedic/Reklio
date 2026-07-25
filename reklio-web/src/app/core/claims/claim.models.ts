import { ClaimStatus } from '../../shared/status-labels';

export type PurchaseType = 'InStore' | 'Online';

export interface ClaimSummary {
  id: number;
  reference: string;
  productName: string;
  status: ClaimStatus;
  submittedAt: string;
}

// AI nalazi — svako polje je null dok odgovarajuća komponenta ne postoji
// (OCR: EPIC 6, vizuelna: EPIC 8, pravilnik: EPIC 7, rizik: EPIC 5).
export interface ClaimAnalysis {
  receiptCheck: string | null;
  damageCheck: string | null;
  policyCheck: string | null;
  riskScore: number | null;
}

export interface ClaimDetail extends ClaimSummary {
  issueType: string;
  issueDescription: string;
  analysis: ClaimAnalysis | null;
}

export interface CreateClaimPayload {
  purchaseType: PurchaseType;
  documentNumber: string;
  receiptFileName: string;
  photoFileNames: string[];
  issueType: string;
  issueDescription: string;
}

export interface CreateClaimResult {
  id: number;
  reference: string;
}
