import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, delay, of } from 'rxjs';
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
    if (environment.useMockData) {
      // Prazna lista — pravi tok kreiranja dolazi kasnije.
      return of<ClaimSummary[]>([]).pipe(delay(300));
    }
    return this.http.get<ClaimSummary[]>(this.baseUrl);
  }

  getClaim(id: number): Observable<ClaimDetail> {
    if (environment.useMockData) {
      return of(this.mockDetail(id)).pipe(delay(300));
    }
    return this.http.get<ClaimDetail>(`${this.baseUrl}/${id}`);
  }

  submitClaim(payload: CreateClaimPayload): Observable<CreateClaimResult> {
    if (environment.useMockData) {
      const id = Math.floor(Math.random() * 9000) + 1000;
      return of({ id, reference: this.buildReference(id) }).pipe(delay(600));
    }
    return this.http.post<CreateClaimResult>(this.baseUrl, payload);
  }

  private mockDetail(id: number): ClaimDetail {
    return {
      id,
      reference: this.buildReference(id),
      productName: 'Pametni telefon X20',
      status: 'Processing',
      submittedAt: new Date().toISOString(),
      issueType: 'Oštećenje pri dostavi',
      issueDescription: 'Ekran je napukao odmah nakon otvaranja paketa.',
      // AI nalazi ne postoje dok pipeline ne bude implementiran (EPIC 5-9).
      analysis: null,
    };
  }

  private buildReference(id: number): string {
    return `REK-${new Date().getFullYear()}-${String(id).padStart(5, '0')}`;
  }
}
