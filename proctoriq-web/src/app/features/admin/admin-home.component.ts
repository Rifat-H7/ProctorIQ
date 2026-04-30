import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../core/auth/auth.service';
import { ExamsApiService, ExamSummary } from '../../core/http/exams-api.service';

@Component({
  selector: 'app-admin-home',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './admin-home.component.html',
  styleUrl: './admin-home.component.scss'
})
export class AdminHomeComponent implements OnInit {
  exams = signal<ExamSummary[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);
  success = signal<string | null>(null);

  selectedExamId = '';

  createModel = {
    title: '',
    durationMinutes: 60,
    startLocal: '',
    endLocal: '',
    totalMarks: 100,
    passMarks: 40,
    isRandomised: true
  };

  questionModel = {
    questionText: '',
    type: 'Mcq' as 'Mcq' | 'TrueFalse',
    marks: 1,
    difficulty: 'Medium',
    topic: 'General',
    optionA: '',
    optionB: '',
    optionC: '',
    optionD: '',
    correctKey: 'A'
  };

  constructor(
    private readonly auth: AuthService,
    private readonly examsApi: ExamsApiService
  ) {}

  ngOnInit(): void {
    this.loadExams();
  }

  loadExams() {
    this.loading.set(true);
    this.error.set(null);
    this.examsApi.list().subscribe({
      next: (items) => {
        this.exams.set(items);
        if (!this.selectedExamId && items.length > 0) this.selectedExamId = items[0].id;
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load exams.');
        this.loading.set(false);
      }
    });
  }

  createExam() {
    this.success.set(null);
    this.error.set(null);

    if (!this.createModel.startLocal || !this.createModel.endLocal) {
      this.error.set('Please provide start and end datetime.');
      return;
    }

    this.examsApi.create({
      title: this.createModel.title,
      durationMinutes: this.createModel.durationMinutes,
      startTimeUtc: new Date(this.createModel.startLocal).toISOString(),
      endTimeUtc: new Date(this.createModel.endLocal).toISOString(),
      totalMarks: this.createModel.totalMarks,
      passMarks: this.createModel.passMarks,
      isRandomised: this.createModel.isRandomised
    }).subscribe({
      next: ({ examId }) => {
        this.success.set('Exam created successfully.');
        this.createModel.title = '';
        this.selectedExamId = examId;
        this.loadExams();
      },
      error: () => this.error.set('Could not create exam.')
    });
  }

  addQuestion() {
    this.success.set(null);
    this.error.set(null);

    if (!this.selectedExamId) {
      this.error.set('Select an exam first.');
      return;
    }

    const options = [
      { key: 'A', text: this.questionModel.optionA },
      { key: 'B', text: this.questionModel.optionB },
      { key: 'C', text: this.questionModel.optionC },
      { key: 'D', text: this.questionModel.optionD }
    ]
      .filter((o) => o.text.trim().length > 0)
      .map((o) => ({ optionText: o.text.trim(), isCorrect: o.key === this.questionModel.correctKey }));

    if (options.length < 2) {
      this.error.set('Add at least two options.');
      return;
    }

    this.examsApi.addQuestion(this.selectedExamId, {
      questionText: this.questionModel.questionText,
      type: this.questionModel.type,
      marks: this.questionModel.marks,
      difficulty: this.questionModel.difficulty,
      topic: this.questionModel.topic,
      options
    }).subscribe({
      next: () => {
        this.success.set('Question added.');
        this.questionModel.questionText = '';
        this.questionModel.optionA = '';
        this.questionModel.optionB = '';
        this.questionModel.optionC = '';
        this.questionModel.optionD = '';
        this.questionModel.correctKey = 'A';
      },
      error: () => this.error.set('Could not add question.')
    });
  }

  logout() {
    this.auth.logout();
  }
}
