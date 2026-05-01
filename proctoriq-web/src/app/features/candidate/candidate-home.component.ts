import { Component, OnDestroy, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../core/auth/auth.service';
import { CandidateApiService, CandidateExamSummary, CandidateQuestion, AttemptResult, AttemptResumeResponse } from '../../core/http/candidate-api.service';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-candidate-home',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './candidate-home.component.html',
  styleUrl: './candidate-home.component.scss'
})
export class CandidateHomeComponent implements OnInit, OnDestroy {
  exams = signal<CandidateExamSummary[]>([]);
  questions = signal<CandidateQuestion[]>([]);
  result = signal<AttemptResult | null>(null);

  loading = signal(false);
  saving = signal(false);
  submitting = signal(false);
  message = signal<string | null>(null);
  error = signal<string | null>(null);
  timerSeconds = signal<number | null>(null);
  isTerminated = signal(false);

  selectedExamId = '';
  attemptId: string | null = null;
  started = false;
  currentIndex = 0;
  answers: Record<string, string | null> = {};

  private hub: signalR.HubConnection | null = null;
  private heartbeatTimer: ReturnType<typeof setInterval> | null = null;
  private tabSwitchCount = 0;
  private lastTabSignalAt = 0;
  private lastLockdownSignalAt: Record<string, number> = {};
  private readonly visibilityHandler = () => {
    if (document.visibilityState === 'hidden') this.notifyTabSwitch();
  };
  private readonly blurHandler = () => this.notifyTabSwitch();
  private readonly keydownHandler = (event: KeyboardEvent) => this.onKeydown(event);
  private readonly clipboardHandler = (event: ClipboardEvent) => this.onClipboardAction(event);
  private readonly contextMenuHandler = (event: MouseEvent) => this.onContextMenu(event);
  private readonly fullscreenHandler = () => this.onFullscreenChanged();

  constructor(
    private readonly auth: AuthService,
    private readonly api: CandidateApiService
  ) {}

  ngOnInit(): void {
    this.loadExams();
  }

  ngOnDestroy(): void {
    this.stopRealtime();
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
    this.isTerminated.set(false);

    this.api.startAttempt(this.selectedExamId).subscribe({
      next: ({ attemptId }) => this.loadAttemptSession({ attemptId, status: 'InProgress', answers: {} }, false),
      error: () => {
        // Resume existing attempt instead of hard failing when it already exists.
        this.api.getAttemptByExam(this.selectedExamId).subscribe({
          next: (resume) => this.loadAttemptSession(resume, true),
          error: () => {
            this.error.set('Could not start or resume attempt.');
            this.loading.set(false);
          }
        });
      }
    });
  }

  private loadAttemptSession(resume: AttemptResumeResponse, resumed: boolean) {
    this.attemptId = resume.attemptId;
    this.api.getQuestions(this.selectedExamId).subscribe({
      next: (questions) => {
        this.questions.set(questions);
        this.answers = {};
        for (const q of questions) this.answers[q.id] = null;

        for (const [questionId, selectedOptionId] of Object.entries(resume.answers ?? {})) {
          this.answers[questionId] = selectedOptionId;
        }

        const firstUnanswered = questions.findIndex((q) => !this.answers[q.id]);
        this.currentIndex = firstUnanswered >= 0 ? firstUnanswered : 0;
        this.started = true;
        this.loading.set(false);
        this.message.set(resumed ? 'Existing attempt resumed.' : 'Attempt started.');
        this.connectRealtime();
      },
      error: () => {
        this.error.set('Attempt ready, but questions could not be loaded.');
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
    if (!q || !this.attemptId || this.isTerminated()) return;

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
    if (!this.attemptId || this.isTerminated()) return;
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
            this.stopRealtime();
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
    this.stopRealtime();
    this.started = false;
    this.questions.set([]);
    this.currentIndex = 0;
    this.answers = {};
    this.result.set(null);
    this.attemptId = null;
    this.timerSeconds.set(null);
    this.message.set(null);
    this.error.set(null);
    this.isTerminated.set(false);
    this.loadExams();
  }

  private async connectRealtime() {
    if (!this.attemptId || !this.selectedExamId) return;

    this.stopRealtime();
    const token = this.auth.accessToken();
    if (!token) return;

    this.hub = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.apiBaseUrl}/hubs/exam`, { accessTokenFactory: () => token })
      .withAutomaticReconnect()
      .build();

    this.hub.on('TimerTick', (_examId: string, seconds: number) => {
      this.timerSeconds.set(seconds);
    });

    this.hub.on('ProctorWarning', (message: string) => {
      this.error.set(`Proctor warning: ${message}`);
    });

    this.hub.on('TerminateSession', (reason: string) => {
      this.error.set(`Session terminated: ${reason}`);
      this.isTerminated.set(true);
      this.started = false;
      this.stopHeartbeat();
      this.stopProctoringSignals();
    });

    try {
      await this.hub.start();
      await this.hub.invoke('JoinAttempt', this.attemptId);
      this.startHeartbeat();
      this.startProctoringSignals();
      this.tryEnterFullscreen();
    } catch {
      this.error.set('Realtime channel could not be connected.');
    }
  }

  private tryEnterFullscreen() {
    const root = document.documentElement;
    if (document.fullscreenElement || !root.requestFullscreen) return;
    root.requestFullscreen().catch(() => {
      this.notifyLockdownSignal('FullscreenExit', 'Fullscreen entry rejected by browser/user');
    });
  }

  private startProctoringSignals() {
    this.stopProctoringSignals();
    this.tabSwitchCount = 0;
    this.lastTabSignalAt = 0;
    this.lastLockdownSignalAt = {};
    document.addEventListener('visibilitychange', this.visibilityHandler);
    window.addEventListener('blur', this.blurHandler);
    window.addEventListener('keydown', this.keydownHandler);
    window.addEventListener('copy', this.clipboardHandler);
    window.addEventListener('cut', this.clipboardHandler);
    window.addEventListener('paste', this.clipboardHandler);
    window.addEventListener('contextmenu', this.contextMenuHandler);
    document.addEventListener('fullscreenchange', this.fullscreenHandler);
  }

  private stopProctoringSignals() {
    document.removeEventListener('visibilitychange', this.visibilityHandler);
    window.removeEventListener('blur', this.blurHandler);
    window.removeEventListener('keydown', this.keydownHandler);
    window.removeEventListener('copy', this.clipboardHandler);
    window.removeEventListener('cut', this.clipboardHandler);
    window.removeEventListener('paste', this.clipboardHandler);
    window.removeEventListener('contextmenu', this.contextMenuHandler);
    document.removeEventListener('fullscreenchange', this.fullscreenHandler);
  }

  private notifyTabSwitch() {
    if (!this.hub || !this.attemptId || !this.started || this.isTerminated()) return;

    const now = Date.now();
    if (now - this.lastTabSignalAt < 1000) return;

    this.lastTabSignalAt = now;
    this.tabSwitchCount += 1;
    this.hub.invoke('TabSwitchDetected', this.attemptId, this.tabSwitchCount).catch(() => undefined);
  }

  private onKeydown(event: KeyboardEvent) {
    if (!this.started || this.isTerminated()) return;

    const key = event.key.toLowerCase();
    const ctrlOrMeta = event.ctrlKey || event.metaKey;
    const forbiddenShortcut = ctrlOrMeta && ['c', 'v', 'x', 'p', 's', 'a'].includes(key);
    const devToolsAttempt = key === 'f12' || (ctrlOrMeta && event.shiftKey && ['i', 'j', 'c'].includes(key));

    if (forbiddenShortcut) {
      event.preventDefault();
      this.notifyLockdownSignal('ForbiddenShortcut', `${ctrlOrMeta ? 'Ctrl/Cmd' : ''}+${key.toUpperCase()}`);
      return;
    }

    if (devToolsAttempt) {
      this.notifyLockdownSignal('DevToolsAttempt', event.key);
    }
  }

  private onClipboardAction(event: ClipboardEvent) {
    if (!this.started || this.isTerminated()) return;
    event.preventDefault();
    this.notifyLockdownSignal('ClipboardAction', event.type);
  }

  private onContextMenu(event: MouseEvent) {
    if (!this.started || this.isTerminated()) return;
    event.preventDefault();
    this.notifyLockdownSignal('ContextMenu', 'Right click disabled');
  }

  private onFullscreenChanged() {
    if (!this.started || this.isTerminated()) return;
    if (!document.fullscreenElement) {
      this.notifyLockdownSignal('FullscreenExit', 'Candidate left fullscreen mode');
    }
  }

  private notifyLockdownSignal(signalType: string, detail: string) {
    if (!this.hub || !this.attemptId) return;

    const now = Date.now();
    const last = this.lastLockdownSignalAt[signalType] ?? 0;
    if (now - last < 1500) return;

    this.lastLockdownSignalAt[signalType] = now;
    this.hub.invoke('LockdownSignal', this.attemptId, signalType, detail).catch(() => undefined);
  }

  private startHeartbeat() {
    if (!this.hub || !this.attemptId) return;
    this.stopHeartbeat();
    this.heartbeatTimer = setInterval(() => {
      this.hub?.invoke('CandidateHeartbeat', this.attemptId).catch(() => undefined);
    }, 15000);
  }

  private stopHeartbeat() {
    if (this.heartbeatTimer) {
      clearInterval(this.heartbeatTimer);
      this.heartbeatTimer = null;
    }
  }

  private stopRealtime() {
    this.stopHeartbeat();
    this.stopProctoringSignals();
    const hub = this.hub;
    this.hub = null;
    if (hub) {
      hub.stop().catch(() => undefined);
    }
  }

  logout() {
    this.stopRealtime();
    this.auth.logout();
  }
}
