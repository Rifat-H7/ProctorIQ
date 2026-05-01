import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

export interface ProctorActiveExam {
  id: string;
  title: string;
  startTimeUtc: string;
  endTimeUtc: string;
  status: string;
}

export interface ProctorAttempt {
  id: string;
  candidateId: string;
  status: string;
  sessionStatus: string;
  lastSeenAtUtc: string | null;
  tabSwitchCount: number;
  warningCount: number;
  score: number | null;
  percentage: number | null;
}

export interface ProctorIncident {
  attemptId: string;
  candidateId?: string;
  proctorId: string;
  eventType: string;
  eventDetail: string;
  loggedAtUtc: string;
  severity?: 'Info' | 'Warning' | 'Critical';
}

export interface ProctorDashboard {
  examId: string;
  summary: {
    totalAttempts: number;
    connected: number;
    suspicious: number;
    disconnected: number;
    submitted: number;
    terminated: number;
    expired: number;
  };
  attempts: ProctorAttempt[];
  recentIncidents: ProctorIncident[];
}

@Injectable({ providedIn: 'root' })
export class ProctorApiService {
  constructor(private readonly http: HttpClient) {}

  listActiveExams() {
    return this.http.get<ProctorActiveExam[]>(`${environment.apiBaseUrl}/api/proctor/exams/active`);
  }

  getDashboard(examId: string) {
    return this.http.get<ProctorDashboard>(`${environment.apiBaseUrl}/api/proctor/exams/${examId}/dashboard`);
  }
}
