import {
  TaskItem,
  Question,
  Attempt,
  StatsOverview,
  HeatmapItem,
  WeakTopicItem,
  ChatMessage,
  ReminderItem,
  LectureItem,
  DriveFileItem,
  DriveProcessResult,
  ModuleItem,
  AdvancedAnalytics,
  ForgettingRiskItem,
  FormulaItem,
  CreateFormulaRequest,
  MockTestStartResponse,
  SubmitMockTestRequest,
  MockTestResult,
  MockTestHistoryItem,
  WeightageOverview,
  HighYieldTopicItem,
  WeightedPaperResponse,
  WeightedPaperSummary,
  GenerateWeightedPaperRequest,
  FlashcardDeck,
  FlashcardItem,
  CreateDeckRequest,
  CreateFlashcardRequest,
  FlashcardDeckStats,
} from '@/types';


const API_BASE = '/api';

async function fetchJson<T>(url: string, options?: RequestInit): Promise<T> {
  const res = await fetch(url, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...options?.headers,
    },
    credentials: 'include',
  });

  if (!res.ok) {
    let errorMsg = `HTTP Error ${res.status}`;
    try {
      const errObj = await res.json();
      errorMsg = errObj.message || errObj.error || errorMsg;
    } catch {
      // ignore
    }
    throw new Error(errorMsg);
  }

  return res.json();
}

export const api = {
  // Auth
  async checkAuth(): Promise<{ authenticated: boolean; message: string }> {
    return fetchJson(`${API_BASE}/auth/me`);
  },

  async login(passphrase: string): Promise<{ authenticated: boolean; message: string }> {
    return fetchJson(`${API_BASE}/auth/login`, {
      method: 'POST',
      body: JSON.stringify({ passphrase }),
    });
  },

  async logout(): Promise<void> {
    await fetchJson(`${API_BASE}/auth/logout`, { method: 'POST' });
  },

  // Tasks
  async getTasks(status?: string, date?: string): Promise<TaskItem[]> {
    const params = new URLSearchParams();
    if (status) params.append('status', status);
    if (date) params.append('date', date);
    const qs = params.toString();
    return fetchJson(`${API_BASE}/tasks${qs ? `?${qs}` : ''}`);
  },

  async createTask(task: { title: string; priority?: string; dueDate?: string; estMin?: number }): Promise<TaskItem> {
    return fetchJson(`${API_BASE}/tasks`, {
      method: 'POST',
      body: JSON.stringify(task),
    });
  },

  async updateTask(id: number, data: Partial<TaskItem>): Promise<TaskItem> {
    return fetchJson(`${API_BASE}/tasks/${id}`, {
      method: 'PATCH',
      body: JSON.stringify(data),
    });
  },

  async deleteTask(id: number): Promise<void> {
    await fetchJson(`${API_BASE}/tasks/${id}`, { method: 'DELETE' });
  },

  // Questions
  async getQuestions(topic?: string, dueOnly: boolean = false): Promise<Question[]> {
    const params = new URLSearchParams();
    if (topic) params.append('topic', topic);
    if (dueOnly) params.append('dueOnly', 'true');
    const qs = params.toString();
    return fetchJson(`${API_BASE}/questions${qs ? `?${qs}` : ''}`);
  },

  async submitAttempt(questionId: number, attempt: { userAnswer: string; timeSec: number; confidence: number }): Promise<Attempt> {
    return fetchJson(`${API_BASE}/questions/${questionId}/attempts`, {
      method: 'POST',
      body: JSON.stringify(attempt),
    });
  },

  // Stats
  async getOverview(): Promise<StatsOverview> {
    return fetchJson(`${API_BASE}/stats/overview`);
  },

  async getHeatmap(days: number = 90): Promise<{ data: HeatmapItem[] }> {
    return fetchJson(`${API_BASE}/stats/heatmap?days=${days}`);
  },

  async getWeakTopics(limit: number = 5): Promise<{ weakTopics: WeakTopicItem[] }> {
    return fetchJson(`${API_BASE}/stats/weak-topics?limit=${limit}`);
  },

  async resetStats(): Promise<{ reset: boolean; message: string }> {
    return fetchJson(`${API_BASE}/stats/reset`, { method: 'POST' });
  },

  // AI Tutor
  async sendAiChat(messages: ChatMessage[], model?: string): Promise<{ role: string; content: string }> {
    return fetchJson(`${API_BASE}/ai/chat`, {
      method: 'POST',
      body: JSON.stringify({ messages, model }),
    });
  },

  async getAiMessages(): Promise<ChatMessage[]> {
    return fetchJson(`${API_BASE}/ai/messages`);
  },

  async generateStudyPlan(days: number = 30): Promise<any> {
    return fetchJson(`${API_BASE}/ai/study-plan`, {
      method: 'POST',
      body: JSON.stringify({ days }),
    });
  },

  // Reminders
  async getReminders(): Promise<ReminderItem[]> {
    return fetchJson(`${API_BASE}/reminders`);
  },

  async toggleReminder(id: number, enabled: boolean): Promise<ReminderItem> {
    return fetchJson(`${API_BASE}/reminders/${id}`, {
      method: 'PATCH',
      body: JSON.stringify({ enabled }),
    });
  },

  // Video Lectures
  async getLectures(): Promise<LectureItem[]> {
    return fetchJson(`${API_BASE}/lectures`);
  },

  async createLecture(data: { title: string; videoUrl: string; totalDurationSec?: number; subjectId?: number }): Promise<LectureItem> {
    return fetchJson(`${API_BASE}/lectures`, {
      method: 'POST',
      body: JSON.stringify(data),
    });
  },

  async updateLectureProgress(id: number, data: { currentTimeSec: number; totalDurationSec?: number; completed?: boolean; notes?: string }): Promise<LectureItem> {
    return fetchJson(`${API_BASE}/lectures/${id}/progress`, {
      method: 'PATCH',
      body: JSON.stringify(data),
    });
  },

  async deleteLecture(id: number): Promise<void> {
    await fetchJson(`${API_BASE}/lectures/${id}`, { method: 'DELETE' });
  },

  // Google Drive & Notes Ingest
  async getDriveStatus(): Promise<any> {
    return fetchJson(`${API_BASE}/drive/status`);
  },

  async getDriveFiles(): Promise<DriveFileItem[]> {
    return fetchJson(`${API_BASE}/drive/files`);
  },

  async syncDrive(): Promise<any> {
    return fetchJson(`${API_BASE}/drive/sync`);
  },

  async ingestDriveFile(data: { fileName: string; content: string; folderPath?: string }): Promise<DriveProcessResult> {
    return fetchJson(`${API_BASE}/drive/ingest`, {
      method: 'POST',
      body: JSON.stringify(data),
    });
  },

  async backupDrive(): Promise<{ success: boolean; backupPath: string }> {
    return fetchJson(`${API_BASE}/drive/backup`, { method: 'POST' });
  },

  // Pluggable Modules
  async getModules(): Promise<ModuleItem[]> {
    return fetchJson(`${API_BASE}/modules`);
  },

  async registerModule(data: { moduleKey: string; title: string; author?: string; description?: string; icon?: string }): Promise<ModuleItem> {
    return fetchJson(`${API_BASE}/modules`, {
      method: 'POST',
      body: JSON.stringify(data),
    });
  },

  async toggleModule(id: number, enabled: boolean): Promise<void> {
    await fetchJson(`${API_BASE}/modules/${id}/toggle`, {
      method: 'PATCH',
      body: JSON.stringify({ enabled }),
    });
  },

  // Advanced Analytics & Forgetting Risk
  async getAdvancedAnalytics(): Promise<AdvancedAnalytics> {
    return fetchJson(`${API_BASE}/stats/advanced`);
  },

  async getForgettingRiskQuestions(limit: number = 5): Promise<ForgettingRiskItem[]> {
    return fetchJson(`${API_BASE}/stats/forgetting-risk?limit=${limit}`);
  },

  // Formula & Theorem Vault Pluggable Module
  async getFormulas(category?: string, search?: string): Promise<FormulaItem[]> {
    const params = new URLSearchParams();
    if (category && category !== 'all') params.append('category', category);
    if (search) params.append('search', search);
    const qs = params.toString();
    return fetchJson(`${API_BASE}/modules/formula-vault${qs ? `?${qs}` : ''}`);
  },

  async getFormulaCategories(): Promise<string[]> {
    return fetchJson(`${API_BASE}/modules/formula-vault/categories`);
  },

  async createFormula(data: CreateFormulaRequest): Promise<FormulaItem> {
    return fetchJson(`${API_BASE}/modules/formula-vault`, {
      method: 'POST',
      body: JSON.stringify(data),
    });
  },

  async deleteFormula(id: number): Promise<void> {
    await fetchJson(`${API_BASE}/modules/formula-vault/${id}`, { method: 'DELETE' });
  },

  // Mock Test & Full Exam Simulation Engine
  async startMockTest(req?: {
    mode?: string;
    questionCount?: number;
    durationMinutes?: number;
    subject?: string;
  }): Promise<MockTestStartResponse> {
    return fetchJson(`${API_BASE}/mock-tests/start`, {
      method: 'POST',
      body: JSON.stringify(req || {}),
    });
  },

  async submitMockTest(sessionId: string, req: SubmitMockTestRequest): Promise<MockTestResult> {
    return fetchJson(`${API_BASE}/mock-tests/${sessionId}/submit`, {
      method: 'POST',
      body: JSON.stringify(req),
    });
  },

  async getMockTestHistory(limit: number = 20): Promise<MockTestHistoryItem[]> {
    return fetchJson(`${API_BASE}/mock-tests/history?limit=${limit}`);
  },

  async getMockTestResult(sessionId: string): Promise<MockTestResult> {
    return fetchJson(`${API_BASE}/mock-tests/${sessionId}`);
  },

  // Weightage & Syllabus Analytics Pluggable Module
  async getWeightageOverview(): Promise<WeightageOverview> {
    return fetchJson(`${API_BASE}/modules/weightage-analysis/overview`);
  },

  async getHighYieldTopics(subject?: string, priority?: string): Promise<HighYieldTopicItem[]> {
    const params = new URLSearchParams();
    if (subject && subject !== 'all') params.append('subject', subject);
    if (priority && priority !== 'all') params.append('priority', priority);
    const qs = params.toString();
    return fetchJson(`${API_BASE}/modules/weightage-analysis/high-yield-topics${qs ? `?${qs}` : ''}`);
  },

  async toggleChecklistItem(id: number, isCompleted?: boolean): Promise<HighYieldTopicItem> {
    return fetchJson(`${API_BASE}/modules/weightage-analysis/checklist/${id}/toggle`, {
      method: 'POST',
      body: JSON.stringify(isCompleted !== undefined ? { isCompleted } : {}),
    });
  },

  async updateChecklistNotes(id: number, notes: string): Promise<HighYieldTopicItem> {
    return fetchJson(`${API_BASE}/modules/weightage-analysis/checklist/${id}/notes`, {
      method: 'POST',
      body: JSON.stringify({ notes }),
    });
  },

  async generateWeightedPaper(req?: GenerateWeightedPaperRequest): Promise<WeightedPaperResponse> {
    return fetchJson(`${API_BASE}/modules/weightage-analysis/generate-paper`, {
      method: 'POST',
      body: JSON.stringify(req || {}),
    });
  },

  async getGeneratedPapers(limit: number = 20): Promise<WeightedPaperSummary[]> {
    return fetchJson(`${API_BASE}/modules/weightage-analysis/papers?limit=${limit}`);
  },

  async getPaperById(paperId: string): Promise<WeightedPaperResponse> {
    return fetchJson(`${API_BASE}/modules/weightage-analysis/papers/${paperId}`);
  },

  // ── Flashcards & Leitner Spaced Repetition Module ────────
  async getFlashcardDecks(): Promise<FlashcardDeck[]> {
    return fetchJson(`${API_BASE}/modules/flashcards/decks`);
  },

  async getFlashcardDeck(id: number): Promise<FlashcardDeck> {
    return fetchJson(`${API_BASE}/modules/flashcards/decks/${id}`);
  },

  async createFlashcardDeck(data: CreateDeckRequest): Promise<FlashcardDeck> {
    return fetchJson(`${API_BASE}/modules/flashcards/decks`, {
      method: 'POST',
      body: JSON.stringify(data),
    });
  },

  async deleteFlashcardDeck(id: number): Promise<void> {
    await fetchJson(`${API_BASE}/modules/flashcards/decks/${id}`, { method: 'DELETE' });
  },

  async getFlashcards(deckId: number, dueOnly?: boolean): Promise<FlashcardItem[]> {
    return fetchJson(`${API_BASE}/modules/flashcards/decks/${deckId}/cards${dueOnly ? '?dueOnly=true' : ''}`);
  },

  async getFlashcardDeckStats(deckId: number): Promise<FlashcardDeckStats> {
    return fetchJson(`${API_BASE}/modules/flashcards/decks/${deckId}/stats`);
  },

  async createFlashcard(data: CreateFlashcardRequest): Promise<FlashcardItem> {
    return fetchJson(`${API_BASE}/modules/flashcards/cards`, {
      method: 'POST',
      body: JSON.stringify(data),
    });
  },

  async reviewFlashcard(id: number, rating: 'again' | 'hard' | 'good' | 'easy'): Promise<FlashcardItem> {
    return fetchJson(`${API_BASE}/modules/flashcards/cards/${id}/review`, {
      method: 'POST',
      body: JSON.stringify({ rating }),
    });
  },

  async deleteFlashcard(id: number): Promise<void> {
    await fetchJson(`${API_BASE}/modules/flashcards/cards/${id}`, { method: 'DELETE' });
  },
};


