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
    type: 'Mcq' as 'Mcq' | 'TrueFalse' | 'Written',
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
    type: 'Mcq' as 'Mcq' | 'TrueFalse' | 'Written',
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

    const options = this.questionModel.type === 'Written' ? [] : this.buildOptions(
      this.questionModel.type,
      this.questionModel.optionA,
      this.questionModel.optionB,
      this.questionModel.optionC,
      this.questionModel.optionD,
      this.questionModel.correctKey
    );

    if (this.questionModel.type !== 'Written' && options.length < 2) {
      this.error.set('Add at least two options.');
      return;
    }
    if (this.questionModel.type === 'TrueFalse' && options.length !== 2) {
      this.error.set('True/False must have exactly two options: True and False.');
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
      type: question.type ?? 'Mcq',
      marks: question.marks,
      difficulty: question.difficulty,
      topic: question.topic,
      optionA: findByIndex(0),
      optionB: findByIndex(1),
      optionC: findByIndex(2),
      optionD: findByIndex(3),
      correctKey: keys[Math.max(0, correctIndex)] ?? 'A'
    };

    if (this.editModel.type === 'TrueFalse') {
      this.applyTrueFalsePreset('edit');
    }
  }

  cancelEdit() {
    this.editingQuestionId = null;
  }

  saveEdit(questionId: string) {
    if (!this.selectedExamId) return;

    const options = this.editModel.type === 'Written' ? [] : this.buildOptions(
      this.editModel.type,
      this.editModel.optionA,
      this.editModel.optionB,
      this.editModel.optionC,
      this.editModel.optionD,
      this.editModel.correctKey
    );

    if (this.editModel.type !== 'Written' && options.length < 2) {
      this.error.set('Edited question must have at least two options.');
      return;
    }
    if (this.editModel.type === 'TrueFalse' && options.length !== 2) {
      this.error.set('True/False must have exactly two options: True and False.');
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

  onCreateTypeChanged() {
    if (this.questionModel.type === 'TrueFalse') {
      this.applyTrueFalsePreset('create');
    }
  }

  onEditTypeChanged() {
    if (this.editModel.type === 'TrueFalse') {
      this.applyTrueFalsePreset('edit');
    }
  }

  private buildOptions(type: 'Mcq' | 'TrueFalse' | 'Written', optionA: string, optionB: string, optionC: string, optionD: string, correctKey: string) {
    if (type === 'TrueFalse') {
      return [
        { optionText: optionA.trim(), isCorrect: correctKey === 'A' },
        { optionText: optionB.trim(), isCorrect: correctKey === 'B' }
      ];
    }

    return [
      { key: 'A', text: optionA },
      { key: 'B', text: optionB },
      { key: 'C', text: optionC },
      { key: 'D', text: optionD }
    ]
      .filter((o) => o.text.trim().length > 0)
      .map((o) => ({ optionText: o.text.trim(), isCorrect: o.key === correctKey }));
  }

  private applyTrueFalsePreset(mode: 'create' | 'edit') {
    if (mode === 'create') {
      this.questionModel.optionA = 'True';
      this.questionModel.optionB = 'False';
      this.questionModel.optionC = '';
      this.questionModel.optionD = '';
      if (this.questionModel.correctKey !== 'A' && this.questionModel.correctKey !== 'B') {
        this.questionModel.correctKey = 'A';
      }
      return;
    }

    this.editModel.optionA = 'True';
    this.editModel.optionB = 'False';
    this.editModel.optionC = '';
    this.editModel.optionD = '';
    if (this.editModel.correctKey !== 'A' && this.editModel.correctKey !== 'B') {
      this.editModel.correctKey = 'A';
    }
  }
}
