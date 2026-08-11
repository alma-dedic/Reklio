import { ClaimStatus } from '../../shared/status-labels';

export type PurchaseType = 'InStore' | 'Online';

export interface ClaimSummary {
  id: number;
  reference: string;
  productName: string;
  status: ClaimStatus;
  submittedAt: string;
}

// AI nalazi — svako polje je null dok pipeline ne završi obradu.
export interface ClaimAnalysis {
  receiptCheck: string | null;
  damageCheck: string | null;
  policyCheck: string | null;
  riskScore: number | null;
}

export interface ClaimDetail extends ClaimSummary {
  issueType: string;
  issueDescription: string;
  explanation: string | null;
  analysis: ClaimAnalysis | null;
}

export interface CreateClaimPayload {
  purchaseType: PurchaseType;
  documentNumber: string;      // '' za fizičku kupovinu (broj čita OCR)
  receiptFile: File | null;    // slika računa za fizičku kupovinu
  photoFiles: File[];
  issueType: string;
  issueDescription: string;
}

export interface CreateClaimResult {
  id: number;
  reference: string;
}
