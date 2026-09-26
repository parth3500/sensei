'use client';

import React, { useState, useEffect, useRef, useCallback } from 'react';
import {
  Timer,
  Award,
  AlertTriangle,
  CheckCircle2,
  XCircle,
  HelpCircle,
  Clock,
  ArrowRight,
  ArrowLeft,
  RotateCcw,
  Play,
  BarChart2,
  ChevronRight,
  Flag,
  Check,
  History,
  TrendingUp,
} from 'lucide-react';
import {
  MockTestStartResponse,
  MockTestQuestion,
  MockTestResult,
  MockTestHistoryItem,
  MockTestAnswerSubmission,
} from '@/types';
import { api } from '@/services/api';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';
import { Badge } from '@/components/ui/Badge';
import { Modal } from '@/components/ui/Modal';

interface MockExamViewProps {
  onBackToDashboard?: () => void;
}

type ExamState = 'launcher' | 'testing' | 'result';

export const MockExamView: React.FC<MockExamViewProps> = ({ onBackToDashboard }) => {
  const [examState, setExamState] = useState<ExamState>('launcher');
  const [loading, setLoading] = useState(false);
  const [history, setHistory] = useState<MockTestHistoryItem[]>([]);

  // Launcher Config State
  const [selectedMode, setSelectedMode] = useState<'quick' | 'subject' | 'full'>('quick');
  const [selectedSubject, setSelectedSubject] = useState<string>('all');

  // Active Session State
  const [activeSession, setActiveSession] = useState<MockTestStartResponse | null>(null);
  const [currentIndex, setCurrentIndex] = useState(0);
  const [userAnswers, setUserAnswers] = useState<Record<number, string>>({});
  const [markedForReview, setMarkedForReview] = useState<Record<number, boolean>>({});
  const [visited, setVisited] = useState<Record<number, boolean>>({ 0: true });
  const [secondsRemaining, setSecondsRemaining] = useState(0);
  const [isSubmitModalOpen, setIsSubmitModalOpen] = useState(false);
  const startTimeRef = useRef<number>(Date.now());
  const timerRef = useRef<NodeJS.Timeout | null>(null);

  // Result State
  const [testResult, setTestResult] = useState<MockTestResult | null>(null);
  const [reviewFilter, setReviewFilter] = useState<'all' | 'incorrect' | 'correct' | 'unattempted'>('all');

  // Load history on mount
  const loadHistory = useCallback(async () => {
    try {
      const hist = await api.getMockTestHistory(10);
      setHistory(hist || []);
    } catch (err) {
      console.error('Failed to load mock test history:', err);
    }
  }, []);

  useEffect(() => {
    loadHistory();
  }, [loadHistory]);

  // Timer Tick
  useEffect(() => {
    if (examState !== 'testing') {
      if (timerRef.current) clearInterval(timerRef.current);
      return;
    }

    timerRef.current = setInterval(() => {
      setSecondsRemaining((prev) => {
        if (prev <= 1) {
          if (timerRef.current) clearInterval(timerRef.current);
          handleAutoSubmit();
          return 0;
        }
        return prev - 1;
      });
    }, 1000);

    return () => {
      if (timerRef.current) clearInterval(timerRef.current);
    };
  }, [examState]);

  // Format MM:SS
  const formatTime = (secs: number) => {
    const m = Math.floor(secs / 60);
    const s = secs % 60;
    return `${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`;
  };

  // Start Test
  const handleStartExam = async () => {
    setLoading(true);
    try {
      const session = await api.startMockTest({
        mode: selectedMode,
        subject: selectedSubject === 'all' ? undefined : selectedSubject,
      });

      setActiveSession(session);
      setCurrentIndex(0);
      setUserAnswers({});
      setMarkedForReview({});
      setVisited({ 0: true });
      setSecondsRemaining(session.durationMinutes * 60);
      startTimeRef.current = Date.now();
      setExamState('testing');
    } catch (err: any) {
      alert(err.message || 'Failed to start mock test');
    } finally {
      setLoading(false);
    }
  };

  // Select Option
  const handleSelectOption = (option: string) => {
    if (!activeSession) return;
    const currentQ = activeSession.questions[currentIndex];
    setUserAnswers((prev) => ({
      ...prev,
      [currentQ.id]: prev[currentQ.id] === option ? '' : option,
    }));
  };

  // Navigation handlers
  const handleNext = () => {
    if (!activeSession) return;
    if (currentIndex < activeSession.questions.length - 1) {
      const nextIdx = currentIndex + 1;
      setCurrentIndex(nextIdx);
      setVisited((prev) => ({ ...prev, [nextIdx]: true }));
    }
  };

  const handlePrev = () => {
    if (currentIndex > 0) {
      const prevIdx = currentIndex - 1;
      setCurrentIndex(prevIdx);
      setVisited((prev) => ({ ...prev, [prevIdx]: true }));
    }
  };

  const handleJumpToQuestion = (index: number) => {
    setCurrentIndex(index);
    setVisited((prev) => ({ ...prev, [index]: true }));
  };

  const handleToggleReview = () => {
    if (!activeSession) return;
    const currentQ = activeSession.questions[currentIndex];
    setMarkedForReview((prev) => ({
      ...prev,
      [currentQ.id]: !prev[currentQ.id],
    }));
  };

  const handleClearResponse = () => {
    if (!activeSession) return;
    const currentQ = activeSession.questions[currentIndex];
    setUserAnswers((prev) => {
      const updated = { ...prev };
      delete updated[currentQ.id];
      return updated;
    });
  };

  // Submit test
  const handleSubmitExam = async () => {
    if (!activeSession) return;
    setIsSubmitModalOpen(false);
    setLoading(true);

    const totalTimeSpentSec = Math.round((Date.now() - startTimeRef.current) / 1000);
    const answers: MockTestAnswerSubmission[] = activeSession.questions.map((q) => ({
      questionId: q.id,
      userAnswer: userAnswers[q.id] || null,
      isMarkedForReview: !!markedForReview[q.id],
      timeSpentSec: 0,
    }));

    try {
      const result = await api.submitMockTest(activeSession.sessionId, {
        answers,
        totalTimeSpentSec,
      });

      setTestResult(result);
      setExamState('result');
      loadHistory();
    } catch (err: any) {
      alert(err.message || 'Error submitting mock exam');
    } finally {
      setLoading(false);
    }
  };

  const handleAutoSubmit = () => {
    handleSubmitExam();
  };

  // View past result
  const handleViewPastResult = async (sessionId: string) => {
    setLoading(true);
    try {
      const res = await api.getMockTestResult(sessionId);
      setTestResult(res);
      setExamState('result');
    } catch (err: any) {
      alert('Could not fetch test result: ' + err.message);
    } finally {
      setLoading(false);
    }
  };

  // Question Status Helper for Palette
  const getQuestionStatus = (idx: number, qId: number) => {
    const isAnswered = !!userAnswers[qId];
    const isReview = !!markedForReview[qId];
    const isCur = currentIndex === idx;
    const hasVisited = !!visited[idx];

    return { isAnswered, isReview, isCur, hasVisited };
  };

  // ─────────────────────────────────────────────────────────────
  // 1. LAUNCHER SCREEN
  // ─────────────────────────────────────────────────────────────
  if (examState === 'launcher') {
    return (
      <div className="space-y-6 pb-24">
        {/* Header */}
        <div className="flex items-center justify-between">
          <div>
            <h2 className="text-2xl font-black text-white tracking-tight flex items-center gap-2">
              <Award className="w-6 h-6 text-accent" />
              GATE Mock Exam Engine
            </h2>
            <p className="text-xs text-zinc-400 mt-0.5">
              Strict exam simulation with official negative marking (+1.00 / -0.33)
            </p>
          </div>
          {onBackToDashboard && (
            <Button variant="secondary" size="sm" onClick={onBackToDashboard}>
              Back
            </Button>
          )}
        </div>

        {/* Mode Selector Cards */}
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
          <div
            onClick={() => setSelectedMode('quick')}
            className={`p-4 rounded-2xl border cursor-pointer transition-all ${
              selectedMode === 'quick'
                ? 'bg-accent/15 border-accent shadow-lg shadow-accent/10'
                : 'bg-surface-card/60 border-surface-border hover:border-zinc-700'
            }`}
          >
            <div className="flex items-center justify-between mb-2">
              <span className="text-xs font-bold uppercase tracking-wider text-accent">Quick Sprint</span>
              <Badge variant="blue">15 Mins</Badge>
            </div>
            <h3 className="text-lg font-bold text-white">10 Questions</h3>
            <p className="text-xs text-zinc-400 mt-1">
              Rapid test across high-yield concepts. Ideal for daily practice sessions.
            </p>
          </div>

          <div
            onClick={() => setSelectedMode('subject')}
            className={`p-4 rounded-2xl border cursor-pointer transition-all ${
              selectedMode === 'subject'
                ? 'bg-accent/15 border-accent shadow-lg shadow-accent/10'
                : 'bg-surface-card/60 border-surface-border hover:border-zinc-700'
            }`}
          >
            <div className="flex items-center justify-between mb-2">
              <span className="text-xs font-bold uppercase tracking-wider text-purple-400">Subject Test</span>
              <Badge variant="purple">45 Mins</Badge>
            </div>
            <h3 className="text-lg font-bold text-white">25 Questions</h3>
            <p className="text-xs text-zinc-400 mt-1">
              Deep dive into specific GATE modules or comprehensive mixed subject testing.
            </p>
          </div>

          <div
            onClick={() => setSelectedMode('full')}
            className={`p-4 rounded-2xl border cursor-pointer transition-all ${
              selectedMode === 'full'
                ? 'bg-accent/15 border-accent shadow-lg shadow-accent/10'
                : 'bg-surface-card/60 border-surface-border hover:border-zinc-700'
            }`}
          >
            <div className="flex items-center justify-between mb-2">
              <span className="text-xs font-bold uppercase tracking-wider text-emerald-400">Full Simulation</span>
              <Badge variant="green">90 Mins</Badge>
            </div>
            <h3 className="text-lg font-bold text-white">Full Syllabus</h3>
            <p className="text-xs text-zinc-400 mt-1">
              Complete syllabus coverage simulating actual GATE examination rigor.
            </p>
          </div>
        </div>

        {/* Subject Filter (Optional) */}
        <Card className="space-y-3">
          <label className="text-xs font-bold text-zinc-300 block">Filter by Focus Subject (Optional):</label>
          <div className="flex flex-wrap gap-2">
            {[
              { id: 'all', label: 'All Subjects (Mixed)' },
              { id: 'Algorithms', label: 'Data Structures & Algorithms' },
              { id: 'Operating Systems', label: 'Operating Systems' },
              { id: 'Computer Networks', label: 'Computer Networks' },
              { id: 'Database', label: 'DBMS' },
              { id: 'Theory of Computation', label: 'Theory of Computation' },
              { id: 'Discrete Mathematics', label: 'Discrete Mathematics' },
              { id: 'Computer Organization', label: 'COA & Digital Logic' },
            ].map((subj) => (
              <button
                key={subj.id}
                onClick={() => setSelectedSubject(subj.id)}
                className={`px-3 py-1.5 rounded-xl text-xs font-medium transition ${
                  selectedSubject === subj.id
                    ? 'bg-accent text-white font-semibold'
                    : 'bg-zinc-800 text-zinc-400 hover:text-white'
                }`}
              >
                {subj.label}
              </button>
            ))}
          </div>
        </Card>

        {/* Marking Scheme Explainer Card */}
        <Card className="bg-gradient-to-r from-blue-900/10 to-indigo-900/10 border-blue-500/20 space-y-2">
          <h4 className="text-xs font-bold text-blue-300 uppercase tracking-wider">Official GATE CS Pattern</h4>
          <div className="grid grid-cols-3 gap-2 text-center text-xs">
            <div className="p-2 rounded-xl bg-zinc-900/60 border border-zinc-800">
              <span className="text-emerald-400 font-bold block text-sm">+1.00</span>
              <span className="text-zinc-500 text-[10px]">Correct Answer</span>
            </div>
            <div className="p-2 rounded-xl bg-zinc-900/60 border border-zinc-800">
              <span className="text-rose-400 font-bold block text-sm">-0.33</span>
              <span className="text-zinc-500 text-[10px]">Wrong Penalty</span>
            </div>
            <div className="p-2 rounded-xl bg-zinc-900/60 border border-zinc-800">
              <span className="text-zinc-400 font-bold block text-sm">0.00</span>
              <span className="text-zinc-500 text-[10px]">Unattempted</span>
            </div>
          </div>
        </Card>

        {/* Start Button */}
        <Button
          onClick={handleStartExam}
          disabled={loading}
          className="w-full py-3.5 text-sm font-bold shadow-xl shadow-accent/20"
        >
          <Play className="w-4 h-4 fill-white" />
          {loading ? 'Generating Exam Paper...' : 'Launch Test Simulator'}
        </Button>

        {/* Past Exam History */}
        {history.length > 0 && (
          <div className="space-y-3 pt-2">
            <h3 className="text-sm font-bold text-white flex items-center gap-2">
              <History className="w-4 h-4 text-zinc-400" />
              Past Exam History
            </h3>
            <div className="space-y-2">
              {history.map((h) => (
                <div
                  key={h.sessionId}
                  onClick={() => handleViewPastResult(h.sessionId)}
                  className="p-3.5 rounded-xl border border-surface-border bg-surface-card/60 hover:bg-surface-card hover:border-zinc-700 cursor-pointer flex items-center justify-between gap-3 transition"
                >
                  <div>
                    <h4 className="text-xs font-bold text-white">{h.title}</h4>
                    <span className="text-[10px] text-zinc-500">
                      {new Date(h.startedAt).toLocaleDateString()} • {h.totalQuestions} Questions
                    </span>
                  </div>
                  <div className="flex items-center gap-3">
                    <div className="text-right">
                      <span className="text-xs font-bold text-accent">
                        {h.totalScore.toFixed(2)} pts ({h.percentage.toFixed(0)}%)
                      </span>
                      <span className="text-[10px] text-zinc-400 block font-mono">
                        Acc: {h.accuracy.toFixed(0)}%
                      </span>
                    </div>
                    <ChevronRight className="w-4 h-4 text-zinc-600" />
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}
      </div>
    );
  }

  // ─────────────────────────────────────────────────────────────
  // 2. LIVE TESTING SCREEN
  // ─────────────────────────────────────────────────────────────
  if (examState === 'testing' && activeSession) {
    const currentQ = activeSession.questions[currentIndex];
    const totalQ = activeSession.questions.length;
    const currentAnswer = userAnswers[currentQ.id] || '';
    const isReview = !!markedForReview[currentQ.id];

    // Counts
    const answeredCount = Object.keys(userAnswers).filter((k) => userAnswers[Number(k)]).length;
    const reviewCount = Object.keys(markedForReview).filter((k) => markedForReview[Number(k)]).length;
    const unansweredCount = totalQ - answeredCount;

    return (
      <div className="space-y-4 pb-24">
        {/* Top Floating Bar */}
        <div className="sticky top-14 z-20 -mx-4 px-4 py-2.5 bg-surface/95 backdrop-blur-xl border-b border-surface-border flex items-center justify-between gap-2 shadow-lg">
          <div>
            <span className="text-xs font-bold text-white">Q {currentIndex + 1} of {totalQ}</span>
            <span className="text-[10px] text-zinc-400 block">{currentQ.topic}</span>
          </div>

          <div className="flex items-center gap-2">
            {/* Timer */}
            <div
              className={`flex items-center gap-1.5 px-3 py-1 rounded-xl text-xs font-mono font-bold border ${
                secondsRemaining < 300
                  ? 'bg-rose-500/20 border-rose-500/50 text-rose-400 animate-pulse'
                  : 'bg-zinc-800/80 border-surface-border text-zinc-200'
              }`}
            >
              <Clock className="w-3.5 h-3.5" />
              <span>{formatTime(secondsRemaining)}</span>
            </div>

            <Button
              variant="primary"
              size="sm"
              onClick={() => setIsSubmitModalOpen(true)}
              className="bg-emerald-600 hover:bg-emerald-500 text-xs px-3"
            >
              Submit
            </Button>
          </div>
        </div>

        {/* Question Area */}
        <Card className="space-y-4 border-surface-border">
          <div className="flex items-center justify-between">
            <Badge variant={currentQ.difficulty === 'hard' ? 'red' : currentQ.difficulty === 'medium' ? 'purple' : 'green'}>
              {currentQ.difficulty.toUpperCase()}
            </Badge>
            <div className="text-[11px] text-zinc-400 font-mono">
              +1.00 / -0.33 Marks
            </div>
          </div>

          {/* Stem */}
          <h3 className="text-sm sm:text-base font-semibold text-white leading-relaxed whitespace-pre-wrap">
            {currentQ.stem}
          </h3>

          {/* Options */}
          <div className="space-y-2 pt-2">
            {currentQ.options.map((option, idx) => {
              const letter = String.fromCharCode(65 + idx);
              const isSelected = currentAnswer === option;

              return (
                <div
                  key={idx}
                  onClick={() => handleSelectOption(option)}
                  className={`p-3.5 rounded-xl border cursor-pointer transition-all flex items-start gap-3 select-none ${
                    isSelected
                      ? 'bg-accent/20 border-accent shadow-md shadow-accent/10'
                      : 'bg-surface-card/60 border-surface-border hover:bg-zinc-800/80'
                  }`}
                >
                  <div
                    className={`w-6 h-6 rounded-lg text-xs font-bold flex items-center justify-center shrink-0 mt-0.5 border ${
                      isSelected
                        ? 'bg-accent text-white border-accent'
                        : 'bg-zinc-800 border-zinc-700 text-zinc-400'
                    }`}
                  >
                    {isSelected ? <Check className="w-3.5 h-3.5" /> : letter}
                  </div>
                  <span className={`text-xs sm:text-sm leading-relaxed ${isSelected ? 'text-white font-medium' : 'text-zinc-300'}`}>
                    {option}
                  </span>
                </div>
              );
            })}
          </div>

          {/* Question Action Controls */}
          <div className="flex flex-wrap items-center justify-between gap-2 pt-2 border-t border-surface-border">
            <div className="flex items-center gap-2">
              <Button
                variant={isReview ? 'primary' : 'secondary'}
                size="sm"
                onClick={handleToggleReview}
                className={isReview ? 'bg-purple-600 hover:bg-purple-500' : ''}
              >
                <Flag className="w-3.5 h-3.5" />
                {isReview ? 'Marked' : 'Mark Review'}
              </Button>
              {currentAnswer && (
                <Button variant="ghost" size="sm" onClick={handleClearResponse} className="text-zinc-500 hover:text-zinc-300">
                  Clear
                </Button>
              )}
            </div>

            <div className="flex items-center gap-2">
              <Button variant="secondary" size="sm" onClick={handlePrev} disabled={currentIndex === 0}>
                <ArrowLeft className="w-3.5 h-3.5" />
                Prev
              </Button>
              <Button
                variant="primary"
                size="sm"
                onClick={handleNext}
                disabled={currentIndex === totalQ - 1}
              >
                Next
                <ArrowRight className="w-3.5 h-3.5" />
              </Button>
            </div>
          </div>
        </Card>

        {/* Question Palette / Grid */}
        <Card className="space-y-3">
          <div className="flex items-center justify-between">
            <h4 className="text-xs font-bold text-zinc-300 uppercase tracking-wider">Question Palette</h4>
            <div className="flex items-center gap-3 text-[10px] text-zinc-400">
              <span className="flex items-center gap-1">
                <span className="w-2 h-2 rounded-full bg-emerald-500 inline-block" /> Answered ({answeredCount})
              </span>
              <span className="flex items-center gap-1">
                <span className="w-2 h-2 rounded-full bg-purple-500 inline-block" /> Review ({reviewCount})
              </span>
              <span className="flex items-center gap-1">
                <span className="w-2 h-2 rounded-full bg-zinc-700 inline-block" /> Left ({unansweredCount})
              </span>
            </div>
          </div>

          <div className="grid grid-cols-5 sm:grid-cols-10 gap-2">
            {activeSession.questions.map((q, idx) => {
              const { isAnswered, isReview, isCur, hasVisited } = getQuestionStatus(idx, q.id);

              let bg = 'bg-zinc-800 text-zinc-400 border-zinc-700';
              if (isReview) {
                bg = 'bg-purple-600/30 text-purple-300 border-purple-500 font-bold';
              } else if (isAnswered) {
                bg = 'bg-emerald-600/30 text-emerald-300 border-emerald-500 font-bold';
              } else if (hasVisited) {
                bg = 'bg-amber-600/20 text-amber-300 border-amber-600/40';
              }

              return (
                <button
                  key={q.id}
                  onClick={() => handleJumpToQuestion(idx)}
                  className={`h-9 rounded-xl border text-xs font-mono transition-transform flex items-center justify-center relative ${bg} ${
                    isCur ? 'ring-2 ring-accent scale-105' : ''
                  }`}
                >
                  {idx + 1}
                  {isReview && (
                    <span className="absolute -top-1 -right-1 w-2 h-2 rounded-full bg-purple-400" />
                  )}
                </button>
              );
            })}
          </div>
        </Card>

        {/* Submit Confirmation Modal */}
        <Modal
          isOpen={isSubmitModalOpen}
          onClose={() => setIsSubmitModalOpen(false)}
          title="Submit Mock Exam?"
        >
          <div className="space-y-4">
            <p className="text-xs text-zinc-300 leading-relaxed">
              Are you sure you want to finish the exam? Once submitted, your score and detailed performance analytics will be computed.
            </p>

            <div className="grid grid-cols-3 gap-2 text-center text-xs">
              <div className="p-3 rounded-xl bg-emerald-500/10 border border-emerald-500/20">
                <span className="text-emerald-400 font-bold text-lg block">{answeredCount}</span>
                <span className="text-zinc-500 text-[10px]">Attempted</span>
              </div>
              <div className="p-3 rounded-xl bg-purple-500/10 border border-purple-500/20">
                <span className="text-purple-400 font-bold text-lg block">{reviewCount}</span>
                <span className="text-zinc-500 text-[10px]">Marked Review</span>
              </div>
              <div className="p-3 rounded-xl bg-zinc-800/60 border border-zinc-700">
                <span className="text-zinc-400 font-bold text-lg block">{unansweredCount}</span>
                <span className="text-zinc-500 text-[10px]">Unanswered</span>
              </div>
            </div>

            <div className="flex justify-end gap-2 pt-2">
              <Button variant="secondary" size="sm" onClick={() => setIsSubmitModalOpen(false)}>
                Return to Exam
              </Button>
              <Button
                variant="primary"
                size="sm"
                onClick={handleSubmitExam}
                disabled={loading}
                className="bg-emerald-600 hover:bg-emerald-500"
              >
                {loading ? 'Evaluating...' : 'Yes, Submit Final Exam'}
              </Button>
            </div>
          </div>
        </Modal>
      </div>
    );
  }

  // ─────────────────────────────────────────────────────────────
  // 3. SCORECARD & REVIEW SCREEN
  // ─────────────────────────────────────────────────────────────
  if (examState === 'result' && testResult) {
    const filteredReview = testResult.questionsReview.filter((q) => {
      if (reviewFilter === 'incorrect') return q.isAttempted && !q.isCorrect;
      if (reviewFilter === 'correct') return q.isCorrect;
      if (reviewFilter === 'unattempted') return !q.isAttempted;
      return true;
    });

    return (
      <div className="space-y-6 pb-24">
        {/* Banner */}
        <div className="p-5 rounded-2xl bg-gradient-to-r from-accent/20 via-purple-900/20 to-zinc-900 border border-accent/40 shadow-xl space-y-4">
          <div className="flex items-center justify-between">
            <div>
              <span className="text-xs font-bold text-accent uppercase tracking-wider">Exam Scorecard</span>
              <h2 className="text-xl sm:text-2xl font-black text-white">{testResult.title}</h2>
            </div>
            <Button
              variant="secondary"
              size="sm"
              onClick={() => {
                setExamState('launcher');
                setTestResult(null);
              }}
            >
              New Test
            </Button>
          </div>

          <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
            <div className="p-3 rounded-xl bg-surface/60 border border-surface-border">
              <span className="text-[11px] text-zinc-400 block font-medium">Net Score</span>
              <span className="text-2xl font-black text-white">{testResult.totalScore.toFixed(2)}</span>
              <span className="text-[10px] text-zinc-500 block">/ {testResult.totalQuestions} Max</span>
            </div>

            <div className="p-3 rounded-xl bg-surface/60 border border-surface-border">
              <span className="text-[11px] text-zinc-400 block font-medium">Accuracy</span>
              <span className="text-2xl font-black text-emerald-400">{testResult.accuracy.toFixed(0)}%</span>
              <span className="text-[10px] text-zinc-500 block">Correct / Attempted</span>
            </div>

            <div className="p-3 rounded-xl bg-surface/60 border border-surface-border">
              <span className="text-[11px] text-zinc-400 block font-medium">Percentile</span>
              <span className="text-2xl font-black text-purple-300">~{testResult.percentileEstimate.toFixed(1)}%</span>
              <span className="text-[10px] text-zinc-500 block">Estimated All-India</span>
            </div>

            <div className="p-3 rounded-xl bg-surface/60 border border-surface-border">
              <span className="text-[11px] text-zinc-400 block font-medium">Avg Time/Q</span>
              <span className="text-2xl font-black text-blue-300">{testResult.avgTimePerQuestionSec}s</span>
              <span className="text-[10px] text-zinc-500 block">Pacing Speed</span>
            </div>
          </div>
        </div>

        {/* Detailed Metrics Breakdown */}
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-3 text-center text-xs">
          <div className="p-3 rounded-xl bg-surface-card/60 border border-surface-border">
            <CheckCircle2 className="w-5 h-5 text-emerald-400 mx-auto mb-1" />
            <span className="text-base font-bold text-white block">{testResult.correctCount}</span>
            <span className="text-zinc-400 text-[11px]">Correct (+{testResult.positiveMarks.toFixed(2)})</span>
          </div>

          <div className="p-3 rounded-xl bg-surface-card/60 border border-surface-border">
            <XCircle className="w-5 h-5 text-rose-400 mx-auto mb-1" />
            <span className="text-base font-bold text-white block">{testResult.wrongCount}</span>
            <span className="text-zinc-400 text-[11px]">Wrong (-{testResult.negativeMarks.toFixed(2)})</span>
          </div>

          <div className="p-3 rounded-xl bg-surface-card/60 border border-surface-border">
            <HelpCircle className="w-5 h-5 text-zinc-400 mx-auto mb-1" />
            <span className="text-base font-bold text-white block">{testResult.unattemptedCount}</span>
            <span className="text-zinc-400 text-[11px]">Unattempted (0.00)</span>
          </div>

          <div className="p-3 rounded-xl bg-surface-card/60 border border-surface-border">
            <TrendingUp className="w-5 h-5 text-accent mx-auto mb-1" />
            <span className="text-base font-bold text-white block">{testResult.rankEstimate}</span>
            <span className="text-zinc-400 text-[11px]">Projected Rank</span>
          </div>
        </div>

        {/* Subject Breakdown */}
        {testResult.subjectBreakdown && testResult.subjectBreakdown.length > 0 && (
          <Card className="space-y-3">
            <h3 className="text-xs font-bold text-zinc-300 uppercase tracking-wider">Subject-Wise Performance</h3>
            <div className="space-y-2">
              {testResult.subjectBreakdown.map((sb, idx) => (
                <div key={idx} className="p-3 rounded-xl bg-zinc-900/60 border border-zinc-800 space-y-1.5">
                  <div className="flex items-center justify-between text-xs">
                    <span className="font-bold text-white">{sb.topic}</span>
                    <span className="font-mono text-accent">{sb.marks.toFixed(2)} pts • {sb.accuracy.toFixed(0)}% acc</span>
                  </div>
                  <div className="w-full h-1.5 rounded-full bg-zinc-800 overflow-hidden">
                    <div
                      className="h-full rounded-full bg-accent transition-all"
                      style={{ width: `${Math.max(0, Math.min(100, sb.accuracy))}%` }}
                    />
                  </div>
                  <div className="flex items-center justify-between text-[10px] text-zinc-500">
                    <span>Total: {sb.total}</span>
                    <span>Correct: {sb.correct} | Wrong: {sb.wrong} | Skipped: {sb.unattempted}</span>
                  </div>
                </div>
              ))}
            </div>
          </Card>
        )}

        {/* Detailed Solutions & Review */}
        <div className="space-y-4">
          <div className="flex flex-wrap items-center justify-between gap-2">
            <h3 className="text-sm font-bold text-white">Question Review & Explanations</h3>
            <div className="flex items-center gap-1">
              {(['all', 'incorrect', 'correct', 'unattempted'] as const).map((filter) => (
                <button
                  key={filter}
                  onClick={() => setReviewFilter(filter)}
                  className={`px-2.5 py-1 rounded-lg text-xs capitalize transition ${
                    reviewFilter === filter
                      ? 'bg-accent text-white font-bold'
                      : 'bg-zinc-800 text-zinc-400 hover:text-white'
                  }`}
                >
                  {filter}
                </button>
              ))}
            </div>
          </div>

          <div className="space-y-3">
            {filteredReview.map((item, idx) => (
              <div
                key={item.questionId}
                className="p-4 rounded-xl border border-surface-border bg-surface-card/60 space-y-3"
              >
                <div className="flex items-center justify-between">
                  <span className="text-xs font-bold text-zinc-400">
                    Question #{idx + 1} • <span className="text-white">{item.topic}</span>
                  </span>
                  <div className="flex items-center gap-2">
                    {item.isCorrect ? (
                      <Badge variant="green">+1.00 Correct</Badge>
                    ) : item.isAttempted ? (
                      <Badge variant="red">-0.33 Incorrect</Badge>
                    ) : (
                      <Badge variant="zinc">Unattempted</Badge>
                    )}
                  </div>
                </div>

                <p className="text-xs sm:text-sm font-medium text-white leading-relaxed">{item.stem}</p>

                {/* Options List */}
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-2 text-xs">
                  {item.options.map((opt, oIdx) => {
                    const isUserPick = item.userAnswer === opt;
                    const isCorrectAnswer = item.correctAnswer === opt;

                    let optClass = 'bg-zinc-900 border-zinc-800 text-zinc-400';
                    if (isCorrectAnswer) {
                      optClass = 'bg-emerald-500/20 border-emerald-500/50 text-emerald-200 font-semibold';
                    } else if (isUserPick && !isCorrectAnswer) {
                      optClass = 'bg-rose-500/20 border-rose-500/50 text-rose-300 line-through';
                    }

                    return (
                      <div key={oIdx} className={`p-2.5 rounded-lg border flex items-center justify-between gap-2 ${optClass}`}>
                        <span>{opt}</span>
                        {isCorrectAnswer && <CheckCircle2 className="w-3.5 h-3.5 text-emerald-400 shrink-0" />}
                        {isUserPick && !isCorrectAnswer && <XCircle className="w-3.5 h-3.5 text-rose-400 shrink-0" />}
                      </div>
                    );
                  })}
                </div>

                {/* Markdown Solution */}
                {item.solutionMd && (
                  <div className="p-3 rounded-xl bg-zinc-900/80 border border-zinc-800 space-y-1">
                    <span className="text-[11px] font-bold text-accent block">Detailed Solution:</span>
                    <p className="text-xs text-zinc-300 leading-relaxed">{item.solutionMd}</p>
                  </div>
                )}
              </div>
            ))}
          </div>
        </div>
      </div>
    );
  }

  return null;
};
