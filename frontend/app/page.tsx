'use client';

import React, { useState, useEffect, useCallback } from 'react';
import { api } from '@/services/api';
import {
  TaskItem,
  Question,
  StatsOverview,
  HeatmapItem,
  WeakTopicItem,
  ChatMessage,
  ReminderItem,
  LectureItem,
  DriveFileItem,
  ModuleItem,
  AdvancedAnalytics,
  ForgettingRiskItem,
  DriveProcessResult
} from '@/types';
import { AppHeader } from '@/components/layout/AppHeader';
import { BottomNav, TabType } from '@/components/layout/BottomNav';
import { AuthModal } from '@/components/features/auth/AuthModal';
import { TodayView } from '@/components/features/today/TodayView';
import { PracticeView } from '@/components/features/practice/PracticeView';
import { LecturesView } from '@/components/features/lectures/LecturesView';
import { DriveStorageView } from '@/components/features/drive/DriveStorageView';
import { ProgressView } from '@/components/features/progress/ProgressView';
import { AiTutorView } from '@/components/features/ai/AiTutorView';
import { ModulesHubView } from '@/components/features/modules/ModulesHubView';
import { SettingsView } from '@/components/features/settings/SettingsView';
import { MockExamView } from '@/components/features/mock/MockExamView';
import { FormulaVaultView } from '@/components/features/vault/FormulaVaultView';
import { WeightageAnalysisView } from '@/components/features/weightage/WeightageAnalysisView';
import { FlashcardDeckView } from '@/components/features/flashcards/FlashcardDeckView';


export default function SenseiApp() {
  const [isAuthenticated, setIsAuthenticated] = useState<boolean | null>(null);
  const [currentTab, setCurrentTab] = useState<TabType>('today');
  const [loading, setLoading] = useState(true);

  // Core Data State
  const [tasks, setTasks] = useState<TaskItem[]>([]);
  const [questions, setQuestions] = useState<Question[]>([]);
  const [stats, setStats] = useState<StatsOverview | null>(null);
  const [heatmap, setHeatmap] = useState<HeatmapItem[]>([]);
  const [weakTopics, setWeakTopics] = useState<WeakTopicItem[]>([]);
  const [reminders, setReminders] = useState<ReminderItem[]>([]);

  // Extended Module Data State
  const [lectures, setLectures] = useState<LectureItem[]>([]);
  const [driveFiles, setDriveFiles] = useState<DriveFileItem[]>([]);
  const [driveStatus, setDriveStatus] = useState<any>(null);
  const [modules, setModules] = useState<ModuleItem[]>([]);
  const [advancedAnalytics, setAdvancedAnalytics] = useState<AdvancedAnalytics | null>(null);
  const [forgettingRisks, setForgettingRisks] = useState<ForgettingRiskItem[]>([]);

  // AI Chat State
  const [chatMessages, setChatMessages] = useState<ChatMessage[]>([
    {
      role: 'assistant',
      content:
        'Namaste Parth! Main tumhara Sensei tutor hoon. GATE CS ki preparation kaisi chal rahi hai? Kisi bhi topic ya formula pe doubt ho toh bejhijhak poocho!',
    },
  ]);
  const [aiLoading, setAiLoading] = useState(false);

  // Load all dashboard & component data
  const loadData = useCallback(async () => {
    try {
      const [
        tList,
        qList,
        sOverview,
        hData,
        wData,
        rList,
        lecList,
        dStatus,
        dFiles,
        modList,
        advAnalytics,
        fRisks,
      ] = await Promise.allSettled([
        api.getTasks(),
        api.getQuestions(),
        api.getOverview(),
        api.getHeatmap(90),
        api.getWeakTopics(5),
        api.getReminders(),
        api.getLectures(),
        api.getDriveStatus(),
        api.getDriveFiles(),
        api.getModules(),
        api.getAdvancedAnalytics(),
        api.getForgettingRiskQuestions(10),
      ]);

      if (tList.status === 'fulfilled') setTasks(tList.value);
      if (qList.status === 'fulfilled') setQuestions(qList.value);
      if (sOverview.status === 'fulfilled') setStats(sOverview.value);
      if (hData.status === 'fulfilled') setHeatmap(hData.value.data || []);
      if (wData.status === 'fulfilled') setWeakTopics(wData.value.weakTopics || []);
      if (rList.status === 'fulfilled') setReminders(rList.value);
      if (lecList.status === 'fulfilled') setLectures(lecList.value);
      if (dStatus.status === 'fulfilled') setDriveStatus(dStatus.value);
      if (dFiles.status === 'fulfilled') setDriveFiles(dFiles.value);
      if (modList.status === 'fulfilled') setModules(modList.value);
      if (advAnalytics.status === 'fulfilled') setAdvancedAnalytics(advAnalytics.value);
      if (fRisks.status === 'fulfilled') setForgettingRisks(fRisks.value);
    } catch (err) {
      console.error('Failed to load dashboard data:', err);
    }
  }, []);

  // Check initial authentication
  useEffect(() => {
    const init = async () => {
      try {
        const res = await api.checkAuth();
        if (res.authenticated) {
          setIsAuthenticated(true);
          await loadData();
        } else {
          setIsAuthenticated(false);
        }
      } catch {
        setIsAuthenticated(false);
      } finally {
        setLoading(false);
      }
    };
    init();
  }, [loadData]);

  // Auth actions
  const handleLogin = async (passphrase: string) => {
    await api.login(passphrase);
    setIsAuthenticated(true);
    await loadData();
  };

  const handleLogout = async () => {
    await api.logout();
    setIsAuthenticated(false);
  };

  // Task actions
  const handleToggleTask = async (task: TaskItem) => {
    const newStatus = task.status === 'done' ? 'pending' : 'done';
    setTasks((prev) =>
      prev.map((t) => (t.id === task.id ? { ...t, status: newStatus } : t))
    );

    try {
      await api.updateTask(task.id, { status: newStatus });
      const [updatedStats, updatedHeatmap] = await Promise.all([
        api.getOverview(),
        api.getHeatmap(90),
      ]);
      setStats(updatedStats);
      setHeatmap(updatedHeatmap.data || []);
    } catch {
      setTasks((prev) =>
        prev.map((t) => (t.id === task.id ? { ...t, status: task.status } : t))
      );
    }
  };

  const handleAddTask = async (taskData: {
    title: string;
    priority: string;
    estMin?: number;
    dueDate?: string;
  }) => {
    const newTask = await api.createTask(taskData);
    setTasks((prev) => [newTask, ...prev]);
    const updatedStats = await api.getOverview();
    setStats(updatedStats);
  };

  const handleBatchAddTasks = async (
    tasksData: Array<{
      title: string;
      priority: string;
      estMin?: number;
      dueDate?: string;
    }>
  ) => {
    try {
      const res = await api.batchCreateTasks(tasksData);
      if (res && res.tasks && res.tasks.length > 0) {
        setTasks((prev) => [...res.tasks, ...prev]);
      } else {
        const refreshedTasks = await api.getTasks();
        setTasks(refreshedTasks);
      }
    } catch {
      for (const item of tasksData) {
        await api.createTask(item);
      }
      const refreshedTasks = await api.getTasks();
      setTasks(refreshedTasks);
    }
    const updatedStats = await api.getOverview();
    setStats(updatedStats);
  };

  const handleDeleteTask = async (id: number) => {
    setTasks((prev) => prev.filter((t) => t.id !== id));
    await api.deleteTask(id);
    const updatedStats = await api.getOverview();
    setStats(updatedStats);
  };

  // Practice actions
  const handleSubmitAnswer = async (
    questionId: number,
    answer: string,
    confidence: number,
    timeSec: number = 15
  ): Promise<boolean> => {
    const attempt = await api.submitAttempt(questionId, {
      userAnswer: answer,
      timeSec,
      confidence,
    });

    const [updatedStats, updatedWeak, updatedAdv, updatedRisks] = await Promise.all([
      api.getOverview(),
      api.getWeakTopics(5),
      api.getAdvancedAnalytics(),
      api.getForgettingRiskQuestions(10),
    ]);
    setStats(updatedStats);
    setWeakTopics(updatedWeak.weakTopics || []);
    setAdvancedAnalytics(updatedAdv);
    setForgettingRisks(updatedRisks);

    return attempt.correct;
  };

  // Lecture actions
  const handleAddLecture = async (data: {
    title: string;
    videoUrl: string;
    totalDurationSec?: number;
  }) => {
    const newLec = await api.createLecture(data);
    setLectures((prev) => [newLec, ...prev]);
  };

  const handleUpdateLectureProgress = async (
    id: number,
    data: {
      currentTimeSec: number;
      totalDurationSec?: number;
      completed?: boolean;
      notes?: string;
    }
  ) => {
    const updated = await api.updateLectureProgress(id, data);
    setLectures((prev) => prev.map((l) => (l.id === id ? updated : l)));
    // Refresh advanced analytics as lecture time impacts study allocation
    try {
      const adv = await api.getAdvancedAnalytics();
      setAdvancedAnalytics(adv);
    } catch {
      // ignore
    }
  };

  const handleDeleteLecture = async (id: number) => {
    setLectures((prev) => prev.filter((l) => l.id !== id));
    await api.deleteLecture(id);
  };

  // Drive Storage actions
  const handleIngestDriveFile = async (data: {
    fileName: string;
    content: string;
  }): Promise<DriveProcessResult> => {
    const result = await api.ingestDriveFile(data);
    // Reload questions, drive files, and advanced analytics
    const [qList, dFiles, adv] = await Promise.all([
      api.getQuestions(),
      api.getDriveFiles(),
      api.getAdvancedAnalytics(),
    ]);
    setQuestions(qList);
    setDriveFiles(dFiles);
    setAdvancedAnalytics(adv);
    return result;
  };

  const handleBackupDrive = async () => {
    return api.backupDrive();
  };

  // Module actions
  const handleRegisterModule = async (data: {
    moduleKey: string;
    title: string;
    author?: string;
    description?: string;
    icon?: string;
  }) => {
    const mod = await api.registerModule(data);
    setModules((prev) => [...prev, mod]);
  };

  const handleToggleModule = async (id: number, enabled: boolean) => {
    setModules((prev) =>
      prev.map((m) => (m.id === id ? { ...m, enabled } : m))
    );
    await api.toggleModule(id, enabled);
  };

  // AI Chat action
  const handleSendMessage = async (content: string) => {
    const userMsg: ChatMessage = { role: 'user', content };
    setChatMessages((prev) => [...prev, userMsg]);
    setAiLoading(true);

    try {
      const response = await api.sendAiChat([...chatMessages, userMsg]);
      setChatMessages((prev) => [...prev, { role: 'assistant', content: response.content }]);
    } catch (err: any) {
      setChatMessages((prev) => [
        ...prev,
        {
          role: 'assistant',
          content: `Arre yaar, error aa gaya: ${err.message}. Thodi der baad try karo!`,
        },
      ]);
    } finally {
      setAiLoading(false);
    }
  };

  // Reminder action
  const handleToggleReminder = async (id: number, enabled: boolean) => {
    setReminders((prev) =>
      prev.map((r) => (r.id === id ? { ...r, enabled } : r))
    );
    await api.toggleReminder(id, enabled);
  };

  const handleResetMetrics = async () => {
    await api.resetStats();
    const [updatedStats, updatedHeatmap, updatedWeak, updatedAdv, updatedRisks, q] = await Promise.all([
      api.getOverview(),
      api.getHeatmap(90),
      api.getWeakTopics(5),
      api.getAdvancedAnalytics(),
      api.getForgettingRiskQuestions(10),
      api.getQuestions(),
    ]);
    setStats(updatedStats);
    setHeatmap(updatedHeatmap.data || []);
    setWeakTopics(updatedWeak.weakTopics || []);
    setAdvancedAnalytics(updatedAdv);
    setForgettingRisks(updatedRisks);
    setQuestions(q);
  };

  if (loading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-[#090a0f]">
        <div className="space-y-4 text-center">
          <div className="w-12 h-12 rounded-2xl bg-accent/20 border border-accent/40 animate-pulse mx-auto flex items-center justify-center">
            <span className="text-accent font-black text-lg">S</span>
          </div>
          <p className="text-xs text-zinc-500 font-medium">Assembling Sensei components...</p>
        </div>
      </div>
    );
  }

  const handleOpenModule = (key: string) => {
    switch (key) {
      case 'flashcards':
        setCurrentTab('flashcards');
        break;
      case 'weightage_analysis':
        setCurrentTab('weightage');
        break;
      case 'formula_vault':
        setCurrentTab('vault');
        break;
      case 'video_tracker':
        setCurrentTab('lectures');
        break;
      case 'drive_notes':
        setCurrentTab('drive');
        break;
      case 'ebbinghaus_decay':
        setCurrentTab('practice');
        break;
      default:
        break;
    }
  };

  return (
    <div className="min-h-screen flex flex-col bg-[#090a0f]">
      {/* Dynamic Header Assembly */}
      <AppHeader stats={stats} currentTab={currentTab} onSelectTab={setCurrentTab} />

      {/* Main Assembly Viewport */}
      <main className="flex-1 max-w-2xl w-full mx-auto px-4 pt-5 pb-16">
        {currentTab === 'today' && (
          <TodayView
            tasks={tasks}
            onToggleTask={handleToggleTask}
            onAddTask={handleAddTask}
            onBatchAddTasks={handleBatchAddTasks}
            onDeleteTask={handleDeleteTask}
          />
        )}

        {currentTab === 'practice' && (
          <PracticeView
            questions={questions}
            forgettingRisks={forgettingRisks}
            onSubmitAnswer={handleSubmitAnswer}
            onRefresh={async () => {
              const [q, r] = await Promise.all([
                api.getQuestions(),
                api.getForgettingRiskQuestions(10),
              ]);
              setQuestions(q);
              setForgettingRisks(r);
            }}
          />
        )}

        {currentTab === 'mock' && (
          <MockExamView onBackToDashboard={() => setCurrentTab('today')} />
        )}

        {currentTab === 'flashcards' && (
          <FlashcardDeckView onBack={() => setCurrentTab('modules')} />
        )}

        {currentTab === 'weightage' && (
          <WeightageAnalysisView
            onBack={() => setCurrentTab('modules')}
            onStartMockExam={(sessionId) => setCurrentTab('mock')}
          />
        )}


        {currentTab === 'vault' && (
          <FormulaVaultView onBack={() => setCurrentTab('modules')} />
        )}

        {currentTab === 'lectures' && (
          <LecturesView
            lectures={lectures}
            onAddLecture={handleAddLecture}
            onUpdateProgress={handleUpdateLectureProgress}
            onDeleteLecture={handleDeleteLecture}
            onRefreshLectures={async () => {
              const l = await api.getLectures();
              setLectures(l);
            }}
          />
        )}

        {currentTab === 'drive' && (
          <DriveStorageView
            driveStatus={driveStatus}
            files={driveFiles}
            onIngestFile={handleIngestDriveFile}
            onBackup={handleBackupDrive}
          />
        )}

        {currentTab === 'progress' && (
          <ProgressView
            stats={stats}
            heatmap={heatmap}
            weakTopics={weakTopics}
            advancedAnalytics={advancedAnalytics}
            onGoToRecall={() => setCurrentTab('practice')}
          />
        )}

        {currentTab === 'ai' && (
          <AiTutorView
            messages={chatMessages}
            onSendMessage={handleSendMessage}
            loading={aiLoading}
          />
        )}

        {currentTab === 'modules' && (
          <ModulesHubView
            modules={modules}
            onRegisterModule={handleRegisterModule}
            onToggleModule={handleToggleModule}
            onOpenModule={handleOpenModule}
          />
        )}

        {currentTab === 'settings' && (
          <SettingsView
            reminders={reminders}
            onToggleReminder={handleToggleReminder}
            onResetMetrics={handleResetMetrics}
            onLogout={handleLogout}
          />
        )}
      </main>

      {/* Bottom Nav Assembly */}
      <BottomNav
        currentTab={currentTab}
        onSelectTab={setCurrentTab}
        pendingDueCount={questions.length}
      />

      {/* Auth Modal (Appears when unauthenticated) */}
      <AuthModal
        isOpen={isAuthenticated === false}
        onSuccess={() => setIsAuthenticated(true)}
        onLogin={handleLogin}
      />
    </div>
  );
}
