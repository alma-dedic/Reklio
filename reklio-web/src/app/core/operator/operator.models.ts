export interface OperatorClaimSummary {
  id: number;
  reference: string;
  productName: string;
  customerName: string;
  riskScore: number | null;
  submittedAt: string;
}

export interface OperatorEvidence {
  id: number;
  type: string;
}

export interface OperatorClaimAnalysis {
  receiptCheck: string | null;
  damageCheck: string | null;
  policyCheck: string | null;
  riskScore: number | null;
}

export interface OperatorClaimDetail {
  id: number;
  reference: string;
  productName: string;
  customerName: string;
  customerEmail: string;
  status: string;
  submittedAt: string;
  issueType: string;
  issueDescription: string;
  analysis: OperatorClaimAnalysis | null;
  operatorExplanation: string | null;
  factors: string[];
  evidence: OperatorEvidence[];
}
