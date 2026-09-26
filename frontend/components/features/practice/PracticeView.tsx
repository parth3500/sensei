import React, { useState, useEffect, useCallback } from 'react';
import { BookOpen, CheckCircle, XCircle, ArrowRight, BrainCircuit, Sparkles, AlertOctagon, RotateCcw, Flame, Timer, SkipForward } from 'lucide-react';
import confetti from 'canvas-confetti';
import { Question, ForgettingRiskItem } from '@/types';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';
import { Badge } from '@/components/ui/Badge';

interface PracticeViewProps {
  questions: Question[];
  forgettingRisks?: ForgettingRiskItem[];
  onSubmitAnswer: (questionId: number, answer: string, confidence: number, timeSec?: number) => Promise<boolean>;
  onRefresh: () => Promise<void>;
}

// Defensive options parser to handle arrays, JSON strings, or line breaks safely
const parseOptions = (raw: any): string[] => {
  if (!raw) return [];
  if (Array.isArray(raw)) return raw.map(String);
  if (typeof raw === 'string') {
    try {
      const parsed = JSON.parse(raw);
      if (Array.isArray(parsed)) return parsed.map(String);
      if (typeof parsed === 'string') return [parsed];
    } catch {
      // Split newline or semicolon separated fallback
      const lines = raw.split(/\r?\n/).map((s: string) => s.trim()).filter(Boolean);
      if (lines.length > 1) return lines;
    }
  }
  return [];
};

// Check if an option matches the expected answer (letter 'A', option text, or prefix)
const isAnswerMatch = (opt: string, idx: number, answer?: string): boolean => {
  if (!answer) return false;
  const cleanAns = answer.trim().toLowerCase();
  const cleanOpt = opt.trim().toLowerCase();

  // 1. Direct match
  if (cleanOpt === cleanAns) return true;

  // 2. Letter match ('A' for index 0, 'B' for index 1, etc.)
  const letter = String.fromCharCode(65 + idx).toLowerCase();
  if (cleanAns === letter) return true;

  // 3. Option prefixed with letter "A) ..." matching answer "A" or text
  if (cleanOpt.startsWith(`${letter})`) || cleanOpt.startsWith(`${letter}.`) || cleanOpt.startsWith(`${letter}:`)) {
    const strippedOpt = cleanOpt.replace(/^[a-d0-9][\)\.\:\-]\s*/, '').trim();
    if (strippedOpt === cleanAns || cleanAns === letter) return true;
  }

  // 4. Answer has letter prefix matching clean option
  const strippedAns = cleanAns.replace(/^[a-d0-9][\)\.\:\-]\s*/, '').trim();
  if (strippedAns === cleanOpt) return true;

  return false;
};

export const PracticeView: React.FC<PracticeViewProps> = ({
  questions,
  forgettingRisks = [],
  onSubmitAnswer,
  onRefresh,
}) => {
  const [mode, setMode] = useState<'standard' | 'forgetting_risk'>('standard');
  const [currentIndex, setCurrentIndex] = useState(0);
  const [selectedOption, setSelectedOption] = useState<string | null>(null);
  const [confidence, setConfidence] = useState(3);
  const [submitted, setSubmitted] = useState(false);
  const [isCorrect, setIsCorrect] = useState<boolean | null>(null);
  const [loading, setLoading] = useState(false);
  const [startTime, setStartTime] = useState<number>(Date.now());
  const [elapsedSec, setElapsedSec] = useState<number>(0);
  const [streak, setStreak] = useState<number>(0);
  const [bestStreak, setBestStreak] = useState<number>(0);

  const activeQuestionList = mode === 'forgetting_risk'
    ? forgettingRisks.map((f) => f.question)
    : questions;

  const currentQ = activeQuestionList.length > 0 ? activeQuestionList[currentIndex] : null;
  const currentRisk = mode === 'forgetting_risk' ? forgettingRisks[currentIndex] : null;
  const options: string[] = parseOptions(currentQ?.optionsJson);

  // Reset timer on question change
  useEffect(() => {
    setStartTime(Date.now());
    setElapsedSec(0);
  }, [currentIndex, mode]);

  // Live timer for question attempt
  useEffect(() => {
    if (submitted || !currentQ) return;
    const interval = setInterval(() => {
      setElapsedSec(Math.max(1, Math.round((Date.now() - startTime) / 1000)));
    }, 1000);
    return () => clearInterval(interval);
  }, [startTime, submitted, currentQ]);

  const handleSelectOption = (opt: string) => {
    if (submitted) return;
    setSelectedOption(opt);
  };

  const handleSubmit = useCallback(async () => {
    if (!currentQ || !selectedOption || submitted || loading) return;

    setLoading(true);
    const timeSpent = Math.max(1, Math.round((Date.now() - startTime) / 1000));
    try {
      const correct = await onSubmitAnswer(currentQ.id, selectedOption, confidence, timeSpent);
      setIsCorrect(correct);
      setSubmitted(true);

      if (correct) {
        setStreak((prev) => {
          const next = prev + 1;
          setBestStreak((b) => Math.max(b, next));
          if (next % 3 === 0 || next === 1) {
            try {
              confetti({
                particleCount: 50,
                spread: 60,
                origin: { y: 0.7 },
                colors: ['#7c5cff', '#10b981', '#f59e0b', '#ec4899'],
              });
            } catch {
              // ignore canvas confetti errors if not supported
            }
          }
          return next;
        });
      } else {
        setStreak(0);
      }
    } finally {
      setLoading(false);
    }
  }, [currentQ, selectedOption, submitted, loading, startTime, onSubmitAnswer, confidence]);

  const handleNext = useCallback(() => {
    setSelectedOption(null);
    setSubmitted(false);
    setIsCorrect(null);
    setConfidence(3);
    setStartTime(Date.now());
    setElapsedSec(0);
    if (currentIndex < activeQuestionList.length - 1) {
      setCurrentIndex(currentIndex + 1);
    } else {
      onRefresh();
      setCurrentIndex(0);
    }
  }, [currentIndex, activeQuestionList.length, onRefresh]);

  const handleSkip = useCallback(() => {
    setSelectedOption(null);
    setSubmitted(false);
    setIsCorrect(null);
    setConfidence(3);
    setStartTime(Date.now());
    setElapsedSec(0);
    if (currentIndex < activeQuestionList.length - 1) {
      setCurrentIndex(currentIndex + 1);
    } else {
      onRefresh();
      setCurrentIndex(0);
    }
  }, [currentIndex, activeQuestionList.length, onRefresh]);

  const handleSwitchMode = (newMode: 'standard' | 'forgetting_risk') => {
    setMode(newMode);
    setCurrentIndex(0);
    setSelectedOption(null);
    setSubmitted(false);
    setIsCorrect(null);
    setConfidence(3);
    setStartTime(Date.now());
    setElapsedSec(0);
  };

  // Keyboard shortcut listener (1-4 / A-D to select, S/Esc to skip, Enter to check / next)
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      // Don't trigger if focus is in an input or textarea
      if (['INPUT', 'TEXTAREA'].includes((e.target as HTMLElement)?.tagName)) return;

      if (!submitted) {
        if (e.key.toLowerCase() === 's' || e.key === 'Escape') {
          e.preventDefault();
          handleSkip();
        } else if (['1', '2', '3', '4'].includes(e.key)) {
          const idx = parseInt(e.key) - 1;
          if (options[idx]) handleSelectOption(options[idx]);
        } else if (['a', 'b', 'c', 'd'].includes(e.key.toLowerCase())) {
          const idx = e.key.toLowerCase().charCodeAt(0) - 97;
          if (options[idx]) handleSelectOption(options[idx]);
        } else if (e.key === 'Enter' && selectedOption && !loading) {
          e.preventDefault();
          handleSubmit();
        }
      } else {
        if (e.key === 'Enter' || e.key === ' ') {
          e.preventDefault();
          handleNext();
        }
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [submitted, options, selectedOption, loading, handleSubmit, handleNext, handleSkip]);

  return (
    <div className="space-y-6 pb-20">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <div>
          <div className="flex items-center gap-2">
            <h2 className="text-2xl font-extrabold text-white tracking-tight">Practice Engine</h2>
            {streak > 0 && (
              <span className="flex items-center gap-1 text-xs font-bold px-2 py-0.5 rounded-full bg-amber-500/20 text-amber-300 border border-amber-500/30 animate-pulse">
                <Flame className="w-3.5 h-3.5 text-amber-400 fill-amber-400" />
                {streak} Streak
              </span>
            )}
          </div>
          <p className="text-xs text-zinc-400">
            {mode === 'standard' ? 'SM-2 Spaced Repetition Due Queue' : 'Active Recall - Forgetting Curve Alert'}
          </p>
        </div>

        {/* Mode Selector Tabs */}
        <div className="flex p-1 bg-zinc-900/90 border border-surface-border rounded-xl">
          <button
            onClick={() => handleSwitchMode('standard')}
            className={`px-3 py-1.5 rounded-lg text-xs font-semibold transition flex items-center gap-1.5 ${
              mode === 'standard'
                ? 'bg-accent text-white shadow-sm'
                : 'text-zinc-400 hover:text-white'
            }`}
          >
            <BookOpen className="w-3.5 h-3.5" />
            Due Queue ({questions.length})
          </button>
          <button
            onClick={() => handleSwitchMode('forgetting_risk')}
            className={`px-3 py-1.5 rounded-lg text-xs font-semibold transition flex items-center gap-1.5 ${
              mode === 'forgetting_risk'
                ? 'bg-rose-500/20 text-rose-300 border border-rose-500/30'
                : 'text-zinc-400 hover:text-white'
            }`}
          >
            <BrainCircuit className="w-3.5 h-3.5 text-rose-400" />
            At-Risk Recall ({forgettingRisks.length})
          </button>
        </div>
      </div>

      {/* Forgetting Curve Risk Banner */}
      {mode === 'forgetting_risk' && currentRisk && (
        <div className="p-3.5 rounded-xl bg-rose-500/10 border border-rose-500/20 flex flex-col sm:flex-row sm:items-center justify-between gap-2 text-xs text-rose-300">
          <div className="flex items-center gap-2">
            <AlertOctagon className="w-4 h-4 text-rose-400 shrink-0" />
            <span>
              Ebbinghaus Decay Alert: Estimated Retention{' '}
              <strong>{Math.round(currentRisk.retentionProbability * 100)}%</strong> &bull; Last reviewed{' '}
              <strong>{currentRisk.daysSinceReview} days ago</strong>
            </span>
          </div>
          <Badge
            variant={
              currentRisk.riskLevel === 'Critical'
                ? 'red'
                : currentRisk.riskLevel === 'High Risk'
                ? 'yellow'
                : 'zinc'
            }
          >
            {currentRisk.riskLevel}
          </Badge>
        </div>
      )}

      {/* Empty State */}
      {!currentQ ? (
        <div className="p-12 text-center rounded-2xl border border-surface-border bg-surface-card/40 space-y-4">
          <div className="w-14 h-14 rounded-2xl bg-emerald-500/10 border border-emerald-500/20 text-emerald-400 mx-auto flex items-center justify-center">
            <Sparkles className="w-7 h-7" />
          </div>
          <h3 className="text-xl font-bold text-white">
            {mode === 'forgetting_risk' ? 'No Concepts at Risk!' : 'All Caught Up!'}
          </h3>
          <p className="text-sm text-zinc-400 max-w-sm mx-auto">
            {mode === 'forgetting_risk'
              ? 'Your memory retention across studied topics is strong. Keep reviewing regularly to prevent decay.'
              : 'No more questions due for review right now. Come back tomorrow or ingest notes from Google Drive!'}
          </p>
          <div className="flex items-center justify-center gap-2 pt-2">
            <Button
              onClick={() => handleSwitchMode(mode === 'standard' ? 'forgetting_risk' : 'standard')}
              variant="secondary"
              size="sm"
            >
              Switch to {mode === 'standard' ? 'At-Risk Recall' : 'Due Queue'}
            </Button>
            <Button onClick={onRefresh} variant="ghost" size="sm">
              <RotateCcw className="w-4 h-4" />
              Refresh
            </Button>
          </div>
        </div>
      ) : (
        /* Question Card */
        <Card className="space-y-5">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <Badge variant="purple">{currentQ.topic}</Badge>
              <Badge
                variant={
                  currentQ.difficulty === 'easy'
                    ? 'green'
                    : currentQ.difficulty === 'medium'
                    ? 'yellow'
                    : 'red'
                }
              >
                {currentQ.difficulty}
              </Badge>
              <span className="flex items-center gap-1 text-[11px] text-zinc-400 font-mono bg-zinc-800/80 px-2 py-0.5 rounded-md border border-zinc-700/50">
                <Timer className="w-3 h-3 text-zinc-400" />
                {elapsedSec}s
              </span>
            </div>
            <div className="flex items-center gap-2">
              <span className="text-[11px] text-zinc-500 font-mono hidden sm:inline">
                Reps: {currentQ.srReps} | Ease: {currentQ.srEase.toFixed(2)}
              </span>
              <span className="text-xs font-semibold px-2 py-0.5 rounded-lg bg-zinc-800 text-zinc-300 border border-zinc-700">
                {currentIndex + 1} / {activeQuestionList.length}
              </span>
            </div>
          </div>

          <p className="text-base font-medium text-zinc-100 leading-relaxed whitespace-pre-line">
            {currentQ.stem}
          </p>

          {/* Options */}
          <div className="space-y-2.5 pt-2">
            {options.map((opt, idx) => {
              const isSelected = selectedOption === opt;
              const isThisAnswer = isAnswerMatch(opt, idx, currentQ.answer);
              const letter = String.fromCharCode(65 + idx);

              let btnStyle = 'border-surface-border bg-zinc-900/60 hover:border-accent/40 text-zinc-200';
              if (submitted) {
                if (isThisAnswer) {
                  btnStyle = 'border-emerald-500 bg-emerald-500/15 text-emerald-200 font-semibold';
                } else if (isSelected && !isCorrect) {
                  btnStyle = 'border-rose-500 bg-rose-500/15 text-rose-200';
                } else {
                  btnStyle = 'border-zinc-800 bg-zinc-900/20 text-zinc-500';
                }
              } else if (isSelected) {
                btnStyle = 'border-accent bg-accent/20 text-white font-semibold ring-1 ring-accent';
              }

              return (
                <button
                  key={idx}
                  disabled={submitted}
                  onClick={() => handleSelectOption(opt)}
                  className={`w-full text-left px-4 py-3 rounded-xl border text-sm transition-all duration-150 flex items-center justify-between group ${btnStyle}`}
                >
                  <div className="flex items-center gap-3">
                    <span className="w-6 h-6 rounded-lg bg-zinc-800/80 border border-zinc-700/60 flex items-center justify-center text-xs font-mono font-semibold text-zinc-400 group-hover:text-white">
                      {letter}
                    </span>
                    <span>{opt}</span>
                  </div>
                  {submitted && isThisAnswer && <CheckCircle className="w-4 h-4 text-emerald-400 shrink-0" />}
                  {submitted && isSelected && !isCorrect && <XCircle className="w-4 h-4 text-rose-400 shrink-0" />}
                </button>
              );
            })}
          </div>

          {/* Confidence Slider */}
          {!submitted && selectedOption && (
            <div className="p-3.5 rounded-xl bg-zinc-900/60 border border-surface-border space-y-2 animate-fade-in">
              <div className="flex justify-between text-xs">
                <span className="text-zinc-400">Confidence Level:</span>
                <span className="text-accent font-semibold">{confidence} / 5</span>
              </div>
              <input
                type="range"
                min="1"
                max="5"
                value={confidence}
                onChange={(e) => setConfidence(parseInt(e.target.value))}
                className="w-full accent-accent h-1.5 bg-zinc-800 rounded-lg cursor-pointer"
              />
              <div className="flex justify-between text-[10px] text-zinc-500">
                <span>Guess (1)</span>
                <span>Neutral (3)</span>
                <span>Certain (5)</span>
              </div>
            </div>
          )}

          {/* Submit or Skip Buttons */}
          <div className="pt-2">
            {!submitted ? (
              <div className="flex items-center gap-2">
                <Button
                  onClick={handleSubmit}
                  disabled={!selectedOption || loading}
                  className="flex-1"
                >
                  {loading ? 'Evaluating...' : 'Check Answer'}
                </Button>
                <Button
                  type="button"
                  variant="secondary"
                  onClick={handleSkip}
                  className="px-4 text-xs font-semibold text-zinc-300 hover:text-white border-zinc-800 bg-zinc-900/80 hover:bg-zinc-800"
                  title="Skip to next question without penalty (Press S or Esc)"
                >
                  <SkipForward className="w-4 h-4 mr-1 text-zinc-400" />
                  Skip
                </Button>
              </div>
            ) : (
              <div className="space-y-4 animate-fade-in">
                <div
                  className={`p-4 rounded-xl border flex items-start gap-3 ${
                    isCorrect
                      ? 'bg-emerald-500/10 border-emerald-500/30 text-emerald-200'
                      : 'bg-rose-500/10 border-rose-500/30 text-rose-200'
                  }`}
                >
                  {isCorrect ? (
                    <CheckCircle className="w-5 h-5 text-emerald-400 shrink-0 mt-0.5" />
                  ) : (
                    <XCircle className="w-5 h-5 text-rose-400 shrink-0 mt-0.5" />
                  )}
                  <div className="text-xs space-y-1 w-full">
                    <div className="flex items-center justify-between">
                      <p className="font-bold text-sm">{isCorrect ? 'Correct Answer!' : 'Incorrect'}</p>
                      {bestStreak > 1 && (
                        <span className="text-[10px] text-zinc-400 font-mono">
                          Best streak: {bestStreak}
                        </span>
                      )}
                    </div>
                    {currentQ.solutionMd && (
                      <p className="text-zinc-300 leading-relaxed whitespace-pre-line pt-1">
                        {currentQ.solutionMd}
                      </p>
                    )}
                  </div>
                </div>

                <Button onClick={handleNext} className="w-full">
                  Next Question
                  <ArrowRight className="w-4 h-4 ml-1.5" />
                </Button>
              </div>
            )}
          </div>
        </Card>
      )}
    </div>
  );
};
