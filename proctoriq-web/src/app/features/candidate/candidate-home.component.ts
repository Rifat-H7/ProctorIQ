import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../core/auth/auth.service';
import { CandidateApiService, CandidateExamSummary, CandidateQuestion, AttemptResult } from '../../core/http/candidate-api.service';

@Component({
  selector: 'app-candidate-home',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './candidate-home.component.html',
  styleUrl: './candidate-home.component.scss'
})
export class CandidateHomeComponent implements OnInit {
  exams = signal<CandidateExamSummary[]>([]);
  questions = signal<CandidateQuestion[]>([]);
  result = signal<AttemptResult | null>(null);

  loading = signal(false);
  saving = signal(false);
  submitting = signal(false);
  message = signal<string | null>(null);
  error = signal<string | null>(null);

  selectedExamId = '';
  attemptId: string | null = null;
  started = false;
  currentIndex = 0;
  answers: Record<string, string | null> = {};

  constructor(
    private readonly auth: AuthService,
    private readonly api: CandidateApiService
  ) {}

  ngOnInit(): void {
    this.loadExams();
  }

  loadExams() {
    this.loading.set(true);
    this.error.set(null);
    this.api.listAvailableExams().subscribe({
      next: (items) => {
        this.exams.set(items);
        if (!this.selectedExamId && items.length > 0) this.selectedExamId = items[0].id;
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load available exams.');
        this.loading.set(false);
      }
    });
  }

  startAttempt() {
    if (!this.selectedExamId) {
      this.error.set('Please select an exam first.');
      return;
    }

    this.loading.set(true);
    this.error.set(null);
    this.message.set(null);
    this.result.set(null);

    this.api.startAttempt(this.selectedExamId).subscribe({
      next: ({ attemptId }) => {
        this.attemptId = attemptId;
        this.api.getQuestions(this.selectedExamId).subscribe({
          next: (questions) => {
            this.questions.set(questions);
            this.answers = {};
            for (const q of questions) this.answers[q.id] = null;
            this.currentIndex = 0;
            this.started = true;
            this.loading.set(false);
            this.message.set('Attempt started. Your answers are saved question-by-question.');
          },
          error: () => {
            this.error.set('Attempt started, but questions could not be loaded.');
            this.loading.set(false);
          }
        });
      },
      error: () => {
        this.error.set('Could not start attempt (you may already have one for this exam).');
        this.loading.set(false);
      }
    });
  }

  get currentQuestion(): CandidateQuestion | null {
    const list = this.questions();
    return list.length > 0 ? list[this.currentIndex] : null;
  }

  selectAnswer(optionId: string) {
    const q = this.currentQuestion;
    if (!q || !this.attemptId) return;

    this.answers[q.id] = optionId;
    this.saving.set(true);
    this.api.saveAnswer(this.attemptId, q.id, optionId).subscribe({
      next: () => {
        this.saving.set(false);
        this.message.set('Answer saved.');
      },
      error: () => {
        this.saving.set(false);
        this.error.set('Failed to save answer.');
      }
    });
  }

  nextQuestion() {
    if (this.currentIndex < this.questions().length - 1) this.currentIndex += 1;
  }

  prevQuestion() {
    if (this.currentIndex > 0) this.currentIndex -= 1;
  }

  submitAttempt() {
    if (!this.attemptId) return;
    this.submitting.set(true);
    this.error.set(null);
    this.message.set(null);

    this.api.submitAttempt(this.attemptId).subscribe({
      next: () => {
        this.api.getResult(this.attemptId!).subscribe({
          next: (res) => {
            this.result.set(res);
            this.submitting.set(false);
            this.started = false;
            this.questions.set([]);
            this.message.set('Attempt submitted successfully.');
          },
          error: () => {
            this.submitting.set(false);
            this.error.set('Submitted, but result could not be loaded yet.');
          }
        });
      },
      error: () => {
        this.submitting.set(false);
        this.error.set('Submit failed.');
      }
    });
  }

  downloadCertificate() {
    if (!this.attemptId) return;
    this.api.downloadCertificate(this.attemptId).subscribe({
      next: (res) => {
        const blob = res.body;
        if (!blob) return;

        const fileName = `certificate-${this.attemptId}.pdf`;
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = fileName;
        a.click();
        window.URL.revokeObjectURL(url);

        const code = res.headers.get('X-Verification-Code');
        this.message.set(code ? `Certificate downloaded. Verification code: ${code}` : 'Certificate downloaded.');
      },
      error: () => this.error.set('Certificate download failed.')
    });
  }

  restartFlow() {
    this.started = false;
    this.questions.set([]);
    this.currentIndex = 0;
    this.answers = {};
    this.result.set(null);
    this.attemptId = null;
    this.message.set(null);
    this.error.set(null);
    this.loadExams();
  }

  logout() {
    this.auth.logout();
  }
}
