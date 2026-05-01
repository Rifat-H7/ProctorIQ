import { Component, OnDestroy, OnInit, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../../environments/environment';
import { AuthService } from '../../core/auth/auth.service';
import { ProctorApiService, ProctorActiveExam, ProctorAttempt, ProctorDashboard, ProctorIncident } from '../../core/http/proctor-api.service';

@Component({
  selector: 'app-proctor-home',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './proctor-home.component.html',
  styleUrl: './proctor-home.component.scss'
})
export class ProctorHomeComponent implements OnInit, OnDestroy {
  exams = signal<ProctorActiveExam[]>([]);
  dashboard = signal<ProctorDashboard | null>(null);
  incidents = signal<ProctorIncident[]>([]);

  loading = signal(false);
  error = signal<string | null>(null);
  info = signal<string | null>(null);

  selectedExamId = '';
  statusFilter = 'All';
  warningMessage = 'Please focus and avoid switching tabs.';
  terminateReason = 'Exam session terminated by proctor.';

  private hub: signalR.HubConnection | null = null;

  readonly filteredAttempts = computed(() => {
    const attempts = this.dashboard()?.attempts ?? [];
    if (this.statusFilter === 'All') return attempts;
    return attempts.filter((a) => a.sessionStatus === this.statusFilter || a.status === this.statusFilter);
  });

  incidentSeverity(incident: ProctorIncident): 'Info' | 'Warning' | 'Critical' {
    if (incident.severity) return incident.severity;
    if (incident.eventType === 'Terminated') return 'Critical';
    if (incident.eventType === 'Warning' || incident.eventType === 'LockdownSignal') return 'Warning';
    return 'Info';
  }

  rowClass(a: ProctorAttempt): string {
    if (a.status === 'Terminated') return 'row-critical';
    if (a.sessionStatus === 'Suspicious') return 'row-warning';
    if (a.sessionStatus === 'Disconnected') return 'row-muted';
    return '';
  }

  badgeClass(a: ProctorAttempt): string {
    if (a.status === 'Terminated') return 'badge critical';
    if (a.sessionStatus === 'Suspicious') return 'badge warning';
    if (a.sessionStatus === 'Disconnected') return 'badge muted';
    if (a.sessionStatus === 'Connected') return 'badge ok';
    if (a.status === 'Submitted') return 'badge info';
    if (a.status === 'Expired') return 'badge muted';
    return 'badge';
  }

  constructor(
    private readonly auth: AuthService,
    private readonly api: ProctorApiService
  ) {}

  ngOnInit(): void {
    this.loadActiveExams();
  }

  ngOnDestroy(): void {
    this.disconnectHub();
  }

  loadActiveExams() {
    this.loading.set(true);
    this.error.set(null);
    this.api.listActiveExams().subscribe({
      next: (items) => {
        this.exams.set(items);
        if (!this.selectedExamId && items.length > 0) this.selectedExamId = items[0].id;
        this.loading.set(false);
        if (this.selectedExamId) this.loadDashboard();
      },
      error: () => {
        this.error.set('Could not load active exams.');
        this.loading.set(false);
      }
    });
  }

  loadDashboard() {
    if (!this.selectedExamId) {
      this.error.set('Please select an exam.');
      return;
    }

    this.loading.set(true);
    this.error.set(null);
    this.api.getDashboard(this.selectedExamId).subscribe({
      next: (data) => {
        this.dashboard.set(data);
        this.incidents.set(data.recentIncidents ?? []);
        this.loading.set(false);
        this.connectHub();
      },
      error: () => {
        this.error.set('Could not load dashboard data.');
        this.loading.set(false);
      }
    });
  }

  async sendWarning(attempt: ProctorAttempt) {
    if (!this.hub) return;
    try {
      await this.hub.invoke('ProctorWarning', attempt.id, this.warningMessage.trim());
      this.info.set(`Warning sent to ${attempt.candidateId}.`);
    } catch {
      this.error.set('Failed to send warning.');
    }
  }

  async terminate(attempt: ProctorAttempt) {
    if (!this.hub) return;
    try {
      await this.hub.invoke('TerminateSession', attempt.id, this.terminateReason.trim());
      this.info.set(`Session terminated for ${attempt.candidateId}.`);
    } catch {
      this.error.set('Failed to terminate session.');
    }
  }

  private connectHub() {
    const token = this.auth.accessToken();
    if (!token || !this.selectedExamId) return;

    this.disconnectHub();

    this.hub = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.apiBaseUrl}/hubs/exam`, { accessTokenFactory: () => token })
      .withAutomaticReconnect()
      .build();

    this.hub.on('CandidateStatusUpdated', (candidateId: string, sessionStatus: string, atUtc: string) => {
      this.patchCandidateStatus(candidateId, sessionStatus, atUtc);
      this.pushIncident({
        attemptId: this.findAttemptIdByCandidate(candidateId),
        candidateId,
        proctorId: '',
        eventType: 'CandidateStatusUpdated',
        eventDetail: sessionStatus,
        loggedAtUtc: atUtc,
        severity: sessionStatus === 'Suspicious' ? 'Warning' : 'Info'
      });
    });

    this.hub.on('TabSwitchDetected', (candidateId: string, switchCount: number, atUtc: string) => {
      this.patchTabSwitch(candidateId, switchCount, atUtc);
      this.pushIncident({
        attemptId: this.findAttemptIdByCandidate(candidateId),
        candidateId,
        proctorId: '',
        eventType: 'TabSwitch',
        eventDetail: `switchCount=${switchCount}`,
        loggedAtUtc: atUtc,
        severity: switchCount >= 3 ? 'Warning' : 'Info'
      });
    });

    this.hub.on('LockdownSignalDetected', (
      candidateId: string,
      attemptId: string,
      signalType: string,
      detail: string,
      atUtc: string,
      sessionStatus: string
    ) => {
      this.patchCandidateStatus(candidateId, sessionStatus, atUtc);
      this.pushIncident({
        attemptId,
        candidateId,
        proctorId: '',
        eventType: 'LockdownSignal',
        eventDetail: `${signalType}${detail ? `: ${detail}` : ''}`,
        loggedAtUtc: atUtc,
        severity: signalType === 'FullscreenExit' || signalType === 'ForbiddenShortcut' ? 'Warning' : 'Info'
      });
    });

    this.hub.start()
      .then(() => this.hub?.invoke('JoinExam', this.selectedExamId, 'Proctor'))
      .catch(() => this.error.set('Realtime connection failed.'));
  }

  private disconnectHub() {
    const hub = this.hub;
    this.hub = null;
    if (hub) {
      hub.stop().catch(() => undefined);
    }
  }

  private patchCandidateStatus(candidateId: string, sessionStatus: string, atUtc: string) {
    const dash = this.dashboard();
    if (!dash) return;
    const updated = dash.attempts.map((a) =>
      a.candidateId === candidateId
        ? { ...a, sessionStatus, lastSeenAtUtc: atUtc }
        : a
    );
    this.dashboard.set({ ...dash, attempts: updated });
  }

  private patchTabSwitch(candidateId: string, switchCount: number, atUtc: string) {
    const dash = this.dashboard();
    if (!dash) return;
    const updated = dash.attempts.map((a) =>
      a.candidateId === candidateId
        ? { ...a, tabSwitchCount: switchCount, lastSeenAtUtc: atUtc }
        : a
    );
    this.dashboard.set({ ...dash, attempts: updated });
  }

  private findAttemptIdByCandidate(candidateId: string): string {
    return this.dashboard()?.attempts.find((a) => a.candidateId === candidateId)?.id ?? '';
  }

  private pushIncident(incident: ProctorIncident) {
    const next = [incident, ...this.incidents()];
    this.incidents.set(next.slice(0, 50));
  }

  logout() {
    this.disconnectHub();
    this.auth.logout();
  }
}
