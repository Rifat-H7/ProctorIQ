import { Injectable } from '@angular/core';
import { HttpClient, HttpResponse } from '@angular/common/http';
import { environment } from '../../../environments/environment';

export interface CandidateExamSummary {
  id: string;
  title: string;
  status: string;
  startTimeUtc: string;
  endTimeUtc: string;
}

export interface CandidateQuestion {
  id: string;
  questionText: string;
  type: string;
  marks: number;
  options: { id: string; optionText: string }[];
}

export interface AttemptResult {
  id: string;
  examId: string;
  score: number | null;
  percentage: number | null;
  isPassed: boolean | null;
  status: string;
}

@Injectable({ providedIn: 'root' })
export class CandidateApiService {
  constructor(private readonly http: HttpClient) {}

  listAvailableExams() {
    return this.http.get<CandidateExamSummary[]>(`${environment.apiBaseUrl}/api/exams/available`);
  }

  startAttempt(examId: string) {
    return this.http.post<{ attemptId: string }>(`${environment.apiBaseUrl}/api/attempts/start`, { examId });
  }

  getQuestions(examId: string) {
    return this.http.get<CandidateQuestion[]>(`${environment.apiBaseUrl}/api/exams/${examId}/questions`);
  }

  saveAnswer(attemptId: string, questionId: string, selectedOptionId: string | null) {
    return this.http.put<void>(`${environment.apiBaseUrl}/api/attempts/${attemptId}/answer`, { questionId, selectedOptionId });
  }

  submitAttempt(attemptId: string) {
    return this.http.post<void>(`${environment.apiBaseUrl}/api/attempts/${attemptId}/submit`, {});
  }

  getResult(attemptId: string) {
    return this.http.get<AttemptResult>(`${environment.apiBaseUrl}/api/results/${attemptId}`);
  }

  downloadCertificate(attemptId: string) {
    return this.http.get(`${environment.apiBaseUrl}/api/certificates/${attemptId}`, {
      responseType: 'blob',
      observe: 'response'
    }) as any as import('rxjs').Observable<HttpResponse<Blob>>;
  }
}
