import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ClaimDetail,
  ClaimSummary,
  CreateClaimPayload,
  CreateClaimResult,
} from './claim.models';

@Injectable({ providedIn: 'root' })
export class ClaimsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/claims`;

  getMyClaims(): Observable<ClaimSummary[]> {
    return this.http.get<ClaimSummary[]>(this.baseUrl);
  }

  getClaim(id: number): Observable<ClaimDetail> {
    return this.http.get<ClaimDetail>(`${this.baseUrl}/${id}`);
  }

  submitClaim(payload: CreateClaimPayload): Observable<CreateClaimResult> {
    // Multipart: slike se šalju kao fajlovi. Content-Type NE postavljamo ručno —
    // browser sam dodaje boundary.
    const form = new FormData();
    form.append('purchaseType', payload.purchaseType);
    form.append('documentNumber', payload.documentNumber ?? '');
    form.append('issueType', payload.issueType);
    form.append('issueDescription', payload.issueDescription);
    if (payload.receiptFile) {
      form.append('receipt', payload.receiptFile);
    }
    for (const photo of payload.photoFiles) {
      form.append('photos', photo);
    }
    return this.http.post<CreateClaimResult>(this.baseUrl, form);
  }
}
