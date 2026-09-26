export interface TaskItem {
  id: number;
  title: string;
  subjectId: number | null;
  topic: string | null;
  dueDate: string | null;
  priority: 'low' | 'medium' | 'high';
  status: 'pending' | 'done';
  estMin: number | null;
  actualMin: number;
  createdAt: string;
  doneAt: string | null;
}

export interface Question {
  id: number;
  subjectId: number | null;
  topic: string;
  difficulty: 'easy' | 'medium' | 'hard';
  stem: string;
  optionsJson: string;
  answer: string;
  solutionMd: string;
  source: string;
  tagsJson: string;
  srEase: number;
  srInterval: number;
  srDue: string;
  srReps: number;
  createdAt: string;
}

export interface Attempt {
  id: number;
  questionId: number;
  userAnswer: string;
  correct: boolean;
  timeSec: number;
  confidence: number;
  notes: string | null;
  createdAt: string;
}

export interface StatsOverview {
  streak: number;
  examCountdownDays: number;
  todayTasksTotal: number;
  todayTasksDone: number;
  accuracy: number;
}

export interface HeatmapItem {
  date: string;
  count: number;
}

export interface WeakTopicItem {
  topic: string;
  totalAttempts: number;
  mistakes: number;
  accuracy: number;
}

export interface ChatMessage {
  role: 'user' | 'assistant' | 'system';
  content: string;
}

export interface ReminderItem {
  id: number;
  title: string;
  cron: string;
  channel: string;
  payloadJson: string;
  enabled: boolean;
  lastFired: string | null;
}

export interface LectureItem {
  id: number;
  title: string;
  videoUrl: string;
  videoId: string | null;
  currentTimeSec: number;
  totalDurationSec: number;
  completed: boolean;
  notes: string | null;
  subjectId: number | null;
  lastWatchedAt: string;
  createdAt: string;
}

export interface DriveFileItem {
  id: number;
  fileName: string;
  fileId: string | null;
  folderPath: string | null;
  fileType: string | null;
  extractedSummary: string | null;
  revisionNotes: string | null;
  generatedQuestionsCount: number;
  syncedAt: string;
}

export interface DriveProcessResult {
  fileName: string;
  summary: string;
  revisionPoints: string[];
  generatedQuestions: Question[];
}

export interface ModuleItem {
  id: number;
  moduleKey: string;
  title: string;
  author: string | null;
  description: string | null;
  icon: string;
  enabled: boolean;
  configJson: string;
  createdAt: string;
}

export interface ForgettingRiskItem {
  question: Question;
  retentionProbability: number;
  daysSinceReview: number;
  riskLevel: 'Critical' | 'High Risk' | 'Moderate';
}

export interface SubjectMastery {
  subject: string;
  totalQuestions: number;
  mastered: number;
  masteryPercentage: number;
}

export interface TimeSpentMetric {
  category: string;
  minutes: number;
}

export interface AdvancedAnalytics {
  overallAccuracy: number;
  retentionRateIndex: number;
  subjectMastery: SubjectMastery[];
  timeSpent: TimeSpentMetric[];
  highRiskTopics: ForgettingRiskItem[];
}

export interface FormulaItem {
  id: number;
  category: string;
  title: string;
  formula: string;
  description: string | null;
  keyVariables: string | null;
  example: string | null;
  createdAt: string;
}

export interface CreateFormulaRequest {
  category: string;
  title: string;
  formula: string;
  description?: string;
  keyVariables?: string;
  example?: string;
}

export interface MockTestQuestion {
  id: number;
  topic: string;
  difficulty: 'easy' | 'medium' | 'hard';
  stem: string;
  options: string[];
}

export interface MockTestStartResponse {
  sessionId: string;
  title: string;
  totalQuestions: number;
  durationMinutes: number;
  startedAt: string;
  questions: MockTestQuestion[];
}

export interface MockTestAnswerSubmission {
  questionId: number;
  userAnswer?: string | null;
  isMarkedForReview?: boolean;
  timeSpentSec?: number;
}

export interface SubmitMockTestRequest {
  answers: MockTestAnswerSubmission[];
  totalTimeSpentSec: number;
}

export interface QuestionReviewItem {
  questionId: number;
  topic: string;
  stem: string;
  options: string[];
  userAnswer: string | null;
  correctAnswer: string;
  isCorrect: boolean;
  isAttempted: boolean;
  isMarkedForReview: boolean;
  timeSpentSec: number;
  marksAwarded: number;
  solutionMd: string;
}

export interface SubjectPerformanceItem {
  topic: string;
  total: number;
  correct: number;
  wrong: number;
  unattempted: number;
  marks: number;
  accuracy: number;
}

export interface MockTestResult {
  sessionId: string;
  title: string;
  totalQuestions: number;
  attemptedCount: number;
  correctCount: number;
  wrongCount: number;
  unattemptedCount: number;
  positiveMarks: number;
  negativeMarks: number;
  totalScore: number;
  percentage: number;
  accuracy: number;
  totalTimeSpentSec: number;
  avgTimePerQuestionSec: number;
  percentileEstimate: number;
  rankEstimate: string;
  subjectBreakdown: SubjectPerformanceItem[];
  questionsReview: QuestionReviewItem[];
}

export interface MockTestHistoryItem {
  sessionId: string;
  title: string;
  totalQuestions: number;
  totalScore: number;
  percentage: number;
  accuracy: number;
  percentileEstimate: number;
  totalTimeSpentSec: number;
  startedAt: string;
  submittedAt: string | null;
  status: string;
}

// ── Weightage & Syllabus Analytics Types ───────────────────

export interface SubjectWeightageItem {
  subjectKey: string;
  subjectName: string;
  weightagePercent: number;
  targetHours: number;
  actualHours: number;
  attemptedCount: number;
  correctCount: number;
  accuracy: number;
  masteryPercentage: number;
  color: string;
  status: string;
  highYieldSummary: string[];
}

export interface HighYieldSummaryDto {
  totalTopics: number;
  completedTopics: number;
  criticalPendingCount: number;
  completionPercentage: number;
}

export interface WeightageOverview {
  subjects: SubjectWeightageItem[];
  totalTargetHours: number;
  totalActualHours: number;
  overallMastery: number;
  highYieldSummary: HighYieldSummaryDto;
}

export interface HighYieldTopicItem {
  id: number;
  subjectKey: string;
  subjectName: string;
  topicName: string;
  priority: 'Critical' | 'High' | 'Medium';
  frequencyScore: number;
  avgMarks: number;
  recurrencePattern: string;
  keyFormulaOrConcept: string;
  isCompleted: boolean;
  notes: string | null;
}

export interface PaperSubjectAllocation {
  subjectKey: string;
  subjectName: string;
  targetPercent: number;
  questionCount: number;
  color: string;
}

export interface WeightedPaperQuestion {
  id: number;
  subjectKey: string;
  subjectName: string;
  topic: string;
  difficulty: 'easy' | 'medium' | 'hard';
  stem: string;
  options: string[];
  correctAnswer?: string;
  solutionMd?: string;
}

export interface WeightedPaperResponse {
  paperId: string;
  title: string;
  totalQuestions: number;
  durationMinutes: number;
  subjectAllocations: PaperSubjectAllocation[];
  questions: WeightedPaperQuestion[];
  createdAt: string;
}

export interface WeightedPaperSummary {
  paperId: string;
  title: string;
  totalQuestions: number;
  durationMinutes: number;
  createdAt: string;
}

export interface GenerateWeightedPaperRequest {
  questionCount?: number;
  durationMinutes?: number;
  title?: string;
  difficulty?: string;
}

// ── Flashcards & Leitner Spaced Repetition Types ──────────
export interface FlashcardDeck {
  id: number;
  title: string;
  description: string | null;
  subject: string | null;
  color: string;
  icon: string;
  cardCount: number;
  dueCount: number;
  boxCounts: Record<number, number>;
  createdAt: string;
}

export interface FlashcardItem {
  id: number;
  deckId: number;
  front: string;
  back: string;
  notes: string | null;
  box: number; // Leitner box 1 to 5
  nextReviewDate: string; // YYYY-MM-DD
  reps: number;
  ease: number;
  intervalDays: number;
  lastReviewedAt: string | null;
  createdAt: string;
}

export interface CreateDeckRequest {
  title: string;
  description?: string;
  subject?: string;
  color?: string;
  icon?: string;
}

export interface CreateFlashcardRequest {
  deckId: number;
  front: string;
  back: string;
  notes?: string;
}

export interface ReviewFlashcardRequest {
  rating: 'again' | 'hard' | 'good' | 'easy';
}

export interface FlashcardDeckStats {
  totalCards: number;
  dueCards: number;
  box1Count: number;
  box2Count: number;
  box3Count: number;
  box4Count: number;
  box5Count: number;
}




