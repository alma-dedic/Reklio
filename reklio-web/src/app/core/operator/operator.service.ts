import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { OperatorClaimDetail, OperatorClaimSummary } from './operator.models';

@Injectable({ providedIn: 'root' })
export class OperatorService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/operator`;

  getEscalated(): Observable<OperatorClaimSummary[]> {
    return this.http.get<OperatorClaimSummary[]>(`${this.baseUrl}/claims`);
  }

  getClaim(id: number): Observable<OperatorClaimDetail> {
    return this.http.get<OperatorClaimDetail>(`${this.baseUrl}/claims/${id}`);
  }

  // Slika dokaza — dohvat kao blob (interceptor doda token), pa objectURL u komponenti.
  getEvidenceBlob(evidenceId: number): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/evidence/${evidenceId}`, { responseType: 'blob' });
  }

  approve(id: number, reason?: string): Observable<unknown> {
    return this.http.post(`${this.baseUrl}/claims/${id}/approve`, { reason: reason ?? null });
  }

  reject(id: number, reason: string): Observable<unknown> {
    return this.http.post(`${this.baseUrl}/claims/${id}/reject`, { reason });
  }
}
