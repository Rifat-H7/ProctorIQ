import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

export interface ExamSummary {
  id: string;
  title: string;
  status: string;
  startTimeUtc: string;
  endTimeUtc: string;
}

export interface CreateExamRequest {
  title: string;
  durationMinutes: number;
  startTimeUtc: string;
  endTimeUtc: string;
  totalMarks: number;
  passMarks: number;
  isRandomised: boolean;
}

export interface OptionRequest {
  optionText: string;
  isCorrect: boolean;
}

export interface AddQuestionRequest {
  examId: string;
  questionText: string;
  type: 'Mcq' | 'TrueFalse';
  marks: number;
  difficulty: string;
  topic: string;
  options: OptionRequest[];
}

@Injectable({ providedIn: 'root' })
export class ExamsApiService {
  constructor(private readonly http: HttpClient) {}

  list() {
    return this.http.get<ExamSummary[]>(`${environment.apiBaseUrl}/api/exams`);
  }

  create(request: CreateExamRequest) {
    return this.http.post<{ examId: string }>(`${environment.apiBaseUrl}/api/exams`, request);
  }

  addQuestion(examId: string, request: Omit<AddQuestionRequest, 'examId'>) {
    return this.http.post<{ questionId: string }>(`${environment.apiBaseUrl}/api/exams/${examId}/questions`, request);
  }
}
