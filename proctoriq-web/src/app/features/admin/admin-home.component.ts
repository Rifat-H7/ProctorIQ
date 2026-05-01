import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../core/auth/auth.service';
import { AdminQuestion, ExamsApiService, ExamSummary } from '../../core/http/exams-api.service';

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
  questions = signal<AdminQuestion[]>([]);
  questionsLoading = signal(false);

  selectedExamId = '';
  editingQuestionId: string | null = null;

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

  editModel = {
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
        if (this.selectedExamId) this.loadQuestions();
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
        this.loadQuestions();
      },
      error: () => this.error.set('Could not add question.')
    });
  }

  onExamChange() {
    this.editingQuestionId = null;
    this.success.set(null);
    this.error.set(null);
    this.loadQuestions();
  }

  loadQuestions() {
    if (!this.selectedExamId) {
      this.questions.set([]);
      return;
    }
    this.questionsLoading.set(true);
    this.examsApi.listQuestionsForAdmin(this.selectedExamId).subscribe({
      next: (items) => {
        this.questions.set(items);
        this.questionsLoading.set(false);
      },
      error: () => {
        this.questionsLoading.set(false);
        this.error.set('Could not load questions for selected exam.');
      }
    });
  }

  startEdit(question: AdminQuestion) {
    this.editingQuestionId = question.id;
    const options = question.options ?? [];
    const findByIndex = (i: number) => options[i]?.optionText ?? '';
    const correct = options.find((x) => x.isCorrect);
    const correctIndex = correct ? options.findIndex((x) => x.id === correct.id) : 0;
    const keys = ['A', 'B', 'C', 'D'];

    this.editModel = {
      questionText: question.questionText,
      type: question.type,
      marks: question.marks,
      difficulty: question.difficulty,
      topic: question.topic,
      optionA: findByIndex(0),
      optionB: findByIndex(1),
      optionC: findByIndex(2),
      optionD: findByIndex(3),
      correctKey: keys[Math.max(0, correctIndex)] ?? 'A'
    };
  }

  cancelEdit() {
    this.editingQuestionId = null;
  }

  saveEdit(questionId: string) {
    if (!this.selectedExamId) return;

    const options = [
      { key: 'A', text: this.editModel.optionA },
      { key: 'B', text: this.editModel.optionB },
      { key: 'C', text: this.editModel.optionC },
      { key: 'D', text: this.editModel.optionD }
    ]
      .filter((o) => o.text.trim().length > 0)
      .map((o) => ({ optionText: o.text.trim(), isCorrect: o.key === this.editModel.correctKey }));

    if (options.length < 2) {
      this.error.set('Edited question must have at least two options.');
      return;
    }

    this.examsApi.updateQuestion(this.selectedExamId, questionId, {
      questionText: this.editModel.questionText,
      type: this.editModel.type,
      marks: this.editModel.marks,
      difficulty: this.editModel.difficulty,
      topic: this.editModel.topic,
      options
    }).subscribe({
      next: () => {
        this.success.set('Question updated.');
        this.editingQuestionId = null;
        this.loadQuestions();
      },
      error: () => this.error.set('Could not update question.')
    });
  }

  logout() {
    this.auth.logout();
  }
}
