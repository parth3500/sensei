'use client';

import React, { useState, useEffect, useCallback } from 'react';
import {
  BarChart3,
  CheckCircle2,
  Circle,
  Sparkles,
  Clock,
  Target,
  ArrowLeft,
  Search,
  Award,
  BookOpen,
  TrendingUp,
  ChevronDown,
  ChevronUp,
  RefreshCw,
  Play,
  FileText,
  AlertTriangle,
  Zap,
  Check,
  CheckSquare
} from 'lucide-react';
import {
  WeightageOverview,
  SubjectWeightageItem,
  HighYieldTopicItem,
  WeightedPaperResponse,
  WeightedPaperSummary
} from '@/types';
import { api } from '@/services/api';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';
import { Badge } from '@/components/ui/Badge';
import { Modal } from '@/components/ui/Modal';

interface WeightageAnalysisViewProps {
  onBack?: () => void;
  onStartMockExam?: (sessionId: string) => void;
}

export const WeightageAnalysisView: React.FC<WeightageAnalysisViewProps> = ({
  onBack,
  onStartMockExam,
}) => {
  const [loading, setLoading] = useState(true);
  const [overview, setOverview] = useState<WeightageOverview | null>(null);
  const [highYieldTopics, setHighYieldTopics] = useState<HighYieldTopicItem[]>([]);
  const [recentPapers, setRecentPapers] = useState<WeightedPaperSummary[]>([]);

  // Filtering state for High-Yield Checklist
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedSubjectFilter, setSelectedSubjectFilter] = useState('all');
  const [selectedPriorityFilter, setSelectedPriorityFilter] = useState('all');
  const [statusFilter, setStatusFilter] = useState<'all' | 'completed' | 'pending'>('all');

  // Expanded subject card in weightage breakdown
  const [expandedSubject, setExpandedSubject] = useState<string | null>(null);

  // Generate Paper Modal state
  const [isGenerateModalOpen, setIsGenerateModalOpen] = useState(false);
  const [generating, setGenerating] = useState(false);
  const [questionCountPreset, setQuestionCountPreset] = useState<number>(25);
  const [paperTitle, setPaperTitle] = useState('');

  // Active Generated Paper state (for interactive view/solve)
  const [activePaper, setActivePaper] = useState<WeightedPaperResponse | null>(null);
  const [paperUserAnswers, setPaperUserAnswers] = useState<Record<number, string>>({});
  const [paperSubmitted, setPaperSubmitted] = useState(false);

  // Load all weightage data
  const loadData = useCallback(async () => {
    setLoading(true);
    try {
      const [ovData, hyData, papersData] = await Promise.allSettled([
        api.getWeightageOverview(),
        api.getHighYieldTopics(),
        api.getGeneratedPapers(10),
      ]);

      if (ovData.status === 'fulfilled') setOverview(ovData.value);
      if (hyData.status === 'fulfilled') setHighYieldTopics(hyData.value);
      if (papersData.status === 'fulfilled') setRecentPapers(papersData.value);
    } catch (err) {
      console.error('Failed to load weightage data:', err);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadData();
  }, [loadData]);

  // Toggle checklist item
  const handleToggleChecklist = async (id: number, currentCompleted: boolean) => {
    // Optimistic UI update
    setHighYieldTopics((prev) =>
      prev.map((item) => (item.id === id ? { ...item, isCompleted: !currentCompleted } : item))
    );

    try {
      const updated = await api.toggleChecklistItem(id, !currentCompleted);
      setHighYieldTopics((prev) =>
        prev.map((item) => (item.id === id ? updated : item))
      );
      // Refresh overview counts in background
      api.getWeightageOverview().then(setOverview).catch(() => {});
    } catch (err) {
      console.error('Failed to toggle checklist item:', err);
      // Revert on error
      setHighYieldTopics((prev) =>
        prev.map((item) => (item.id === id ? { ...item, isCompleted: currentCompleted } : item))
      );
    }
  };

  // Generate Paper
  const handleGeneratePaper = async () => {
    setGenerating(true);
    try {
      const paper = await api.generateWeightedPaper({
        questionCount: questionCountPreset,
        title: paperTitle.trim() || undefined,
      });
      setActivePaper(paper);
      setPaperUserAnswers({});
      setPaperSubmitted(false);
      setIsGenerateModalOpen(false);
      // Refresh papers list
      const papers = await api.getGeneratedPapers(10);
      setRecentPapers(papers);
    } catch (err) {
      console.error('Failed to generate weighted paper:', err);
      alert('Failed to generate weighted paper. Please try again.');
    } finally {
      setGenerating(false);
    }
  };

  // View existing paper
  const handleViewPaper = async (paperId: string) => {
    try {
      const paper = await api.getPaperById(paperId);
      setActivePaper(paper);
      setPaperUserAnswers({});
      setPaperSubmitted(false);
    } catch (err) {
      console.error('Failed to fetch paper details:', err);
    }
  };

  // Filter high-yield topics
  const filteredTopics = highYieldTopics.filter((topic) => {
    if (selectedSubjectFilter !== 'all' && topic.subjectKey !== selectedSubjectFilter) {
      return false;
    }
    if (selectedPriorityFilter !== 'all' && topic.priority !== selectedPriorityFilter) {
      return false;
    }
    if (statusFilter === 'completed' && !topic.isCompleted) return false;
    if (statusFilter === 'pending' && topic.isCompleted) return false;
    if (searchQuery.trim()) {
      const q = searchQuery.toLowerCase();
      const matchName = topic.topicName.toLowerCase().includes(q);
      const matchFormula = topic.keyFormulaOrConcept.toLowerCase().includes(q);
      const matchPattern = topic.recurrencePattern.toLowerCase().includes(q);
      const matchSubj = topic.subjectName.toLowerCase().includes(q);
      if (!matchName && !matchFormula && !matchPattern && !matchSubj) return false;
    }
    return true;
  });

  // Calculate score for active paper
  const computePaperResults = () => {
    if (!activePaper) return { correct: 0, wrong: 0, unattempted: 0, score: 0, accuracy: 0 };
    let correct = 0;
    let wrong = 0;
    let unattempted = 0;

    activePaper.questions.forEach((q) => {
      const ans = paperUserAnswers[q.id];
      if (!ans) {
        unattempted++;
      } else if (ans === q.correctAnswer) {
        correct++;
      } else {
        wrong++;
      }
    });

    // GATE standard marking: 1 mark per question, -0.33 negative marking
    const score = Math.max(0, Math.round((correct * 1.0 - wrong * 0.33) * 100) / 100);
    const attempted = correct + wrong;
    const accuracy = attempted > 0 ? Math.round((correct / attempted) * 100) : 0;

    return { correct, wrong, unattempted, score, accuracy };
  };

  if (loading && !overview) {
    return (
      <div className="flex flex-col items-center justify-center min-h-[400px] space-y-4">
        <div className="w-12 h-12 rounded-2xl bg-accent/20 border border-accent/40 animate-pulse flex items-center justify-center">
          <BarChart3 className="w-6 h-6 text-accent animate-spin" />
        </div>
        <p className="text-xs text-zinc-400">Loading GATE CS Syllabus Weightage & Analytics...</p>
      </div>
    );
  }

  const paperResults = paperSubmitted ? computePaperResults() : null;

  return (
    <div className="space-y-6 pb-20">
      {/* Top Header & Navigation */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div className="flex items-center gap-3">
          {onBack && (
            <button
              onClick={onBack}
              className="p-2 rounded-xl bg-zinc-800/80 border border-zinc-700/80 text-zinc-400 hover:text-white transition"
              title="Back"
            >
              <ArrowLeft className="w-4 h-4" />
            </button>
          )}
          <div>
            <div className="flex items-center gap-2">
              <h2 className="text-xl sm:text-2xl font-black text-white tracking-tight">
                Weightage & Syllabus Analytics
              </h2>
              <Badge variant="purple">GATE CS</Badge>
            </div>
            <p className="text-xs text-zinc-400">
              Historical weightage distribution, high-yield checklist & 1-click test generator
            </p>
          </div>
        </div>

        <div className="flex items-center gap-2">
          <Button
            onClick={() => setIsGenerateModalOpen(true)}
            size="sm"
            className="bg-gradient-to-r from-accent to-purple-600 shadow-lg shadow-accent/20 font-bold"
          >
            <Sparkles className="w-4 h-4" />
            Generate Weighted Paper
          </Button>
          <button
            onClick={loadData}
            className="p-2 rounded-xl bg-zinc-800/80 border border-zinc-700/80 text-zinc-400 hover:text-white transition"
            title="Refresh Data"
          >
            <RefreshCw className="w-4 h-4" />
          </button>
        </div>
      </div>

      {/* KPI Overview Cards */}
      {overview && (
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
          <Card className="p-3.5 bg-surface-card/60 border-surface-border">
            <div className="flex items-center justify-between text-zinc-400 mb-1">
              <span className="text-[11px] font-medium">Prep Hours</span>
              <Clock className="w-3.5 h-3.5 text-accent" />
            </div>
            <div className="flex items-baseline gap-1.5">
              <span className="text-lg font-black text-white">{overview.totalActualHours}h</span>
              <span className="text-xs text-zinc-500 font-mono">/ {overview.totalTargetHours}h</span>
            </div>
            <div className="w-full bg-zinc-800 h-1.5 rounded-full mt-2 overflow-hidden">
              <div
                className="bg-accent h-full rounded-full transition-all duration-500"
                style={{
                  width: `${Math.min(100, (overview.totalActualHours / overview.totalTargetHours) * 100)}%`,
                }}
              />
            </div>
          </Card>

          <Card className="p-3.5 bg-surface-card/60 border-surface-border">
            <div className="flex items-center justify-between text-zinc-400 mb-1">
              <span className="text-[11px] font-medium">Syllabus Mastery</span>
              <Target className="w-3.5 h-3.5 text-emerald-400" />
            </div>
            <div className="flex items-baseline gap-1.5">
              <span className="text-lg font-black text-emerald-400">{overview.overallMastery}%</span>
              <span className="text-xs text-zinc-500">score</span>
            </div>
            <div className="w-full bg-zinc-800 h-1.5 rounded-full mt-2 overflow-hidden">
              <div
                className="bg-emerald-500 h-full rounded-full transition-all duration-500"
                style={{ width: `${Math.min(100, overview.overallMastery)}%` }}
              />
            </div>
          </Card>

          <Card className="p-3.5 bg-surface-card/60 border-surface-border">
            <div className="flex items-center justify-between text-zinc-400 mb-1">
              <span className="text-[11px] font-medium">High-Yield Checklist</span>
              <CheckCircle2 className="w-3.5 h-3.5 text-purple-400" />
            </div>
            <div className="flex items-baseline gap-1.5">
              <span className="text-lg font-black text-white">
                {overview.highYieldSummary.completedTopics}
              </span>
              <span className="text-xs text-zinc-500 font-mono">
                / {overview.highYieldSummary.totalTopics}
              </span>
            </div>
            <div className="w-full bg-zinc-800 h-1.5 rounded-full mt-2 overflow-hidden">
              <div
                className="bg-purple-500 h-full rounded-full transition-all duration-500"
                style={{ width: `${overview.highYieldSummary.completionPercentage}%` }}
              />
            </div>
          </Card>

          <Card className="p-3.5 bg-surface-card/60 border-surface-border">
            <div className="flex items-center justify-between text-zinc-400 mb-1">
              <span className="text-[11px] font-medium">Critical Pending</span>
              <AlertTriangle className="w-3.5 h-3.5 text-rose-400" />
            </div>
            <div className="flex items-baseline gap-1.5">
              <span className="text-lg font-black text-rose-400">
                {overview.highYieldSummary.criticalPendingCount}
              </span>
              <span className="text-xs text-zinc-500">must-do topics</span>
            </div>
            <div className="text-[10px] text-zinc-400 mt-2 truncate">
              Highest scoring PYQ subtopics
            </div>
          </Card>
        </div>
      )}

      {/* Active Generated Paper Runner/Viewer */}
      {activePaper && (
        <Card className="p-5 border-accent/40 bg-gradient-to-b from-surface-card to-zinc-950 shadow-xl space-y-4">
          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 border-b border-zinc-800 pb-4">
            <div>
              <div className="flex items-center gap-2">
                <Badge variant="purple">Paper Generated</Badge>
                <h3 className="text-base font-bold text-white">{activePaper.title}</h3>
              </div>
              <div className="flex items-center gap-4 text-xs text-zinc-400 mt-1 font-mono">
                <span>{activePaper.totalQuestions} Questions</span>
                <span>{activePaper.durationMinutes} Minutes</span>
                <span>ID: {activePaper.paperId}</span>
              </div>
            </div>

            <div className="flex items-center gap-2">
              {onStartMockExam && (
                <Button
                  onClick={() => onStartMockExam(activePaper.paperId)}
                  size="sm"
                  variant="secondary"
                  className="text-xs"
                >
                  <Play className="w-3.5 h-3.5 mr-1" />
                  Exam Engine
                </Button>
              )}
              <Button
                onClick={() => setActivePaper(null)}
                size="sm"
                variant="ghost"
                className="text-xs text-zinc-400"
              >
                Close Paper
              </Button>
            </div>
          </div>

          {/* Paper Subject Distribution Badges */}
          <div className="space-y-1.5">
            <span className="text-xs font-semibold text-zinc-400">Weighted Question Allocation:</span>
            <div className="flex flex-wrap gap-2">
              {activePaper.subjectAllocations.map((alloc) => (
                <div
                  key={alloc.subjectKey}
                  className="px-2.5 py-1 rounded-lg text-xs font-semibold flex items-center gap-1.5 border border-zinc-800 bg-zinc-900/80"
                >
                  <span
                    className="w-2 h-2 rounded-full"
                    style={{ backgroundColor: alloc.color }}
                  />
                  <span className="text-zinc-300">{alloc.subjectName}:</span>
                  <span className="text-white font-mono">{alloc.questionCount} Qs</span>
                  <span className="text-zinc-500 font-mono text-[10px]">({alloc.targetPercent}%)</span>
                </div>
              ))}
            </div>
          </div>

          {/* Results Banner if Submitted */}
          {paperSubmitted && paperResults && (
            <div className="p-4 rounded-xl border border-emerald-500/30 bg-emerald-500/10 grid grid-cols-2 sm:grid-cols-4 gap-3 text-center">
              <div>
                <span className="text-xs text-zinc-400">Score</span>
                <p className="text-lg font-black text-emerald-400">
                  {paperResults.score} / {activePaper.totalQuestions}
                </p>
              </div>
              <div>
                <span className="text-xs text-zinc-400">Accuracy</span>
                <p className="text-lg font-black text-white">{paperResults.accuracy}%</p>
              </div>
              <div>
                <span className="text-xs text-zinc-400">Correct / Wrong</span>
                <p className="text-lg font-black text-white">
                  <span className="text-emerald-400">{paperResults.correct}</span> /{' '}
                  <span className="text-rose-400">{paperResults.wrong}</span>
                </p>
              </div>
              <div>
                <span className="text-xs text-zinc-400">Unattempted</span>
                <p className="text-lg font-black text-zinc-400">{paperResults.unattempted}</p>
              </div>
            </div>
          )}

          {/* Questions List */}
          <div className="space-y-4 max-h-[500px] overflow-y-auto pr-1">
            {activePaper.questions.map((q, idx) => {
              const selectedAns = paperUserAnswers[q.id];
              const isCorrect = paperSubmitted && selectedAns === q.correctAnswer;
              const isWrong = paperSubmitted && selectedAns && selectedAns !== q.correctAnswer;

              return (
                <div
                  key={q.id}
                  className="p-4 rounded-xl border border-zinc-800 bg-zinc-900/60 space-y-3"
                >
                  <div className="flex items-center justify-between text-xs">
                    <div className="flex items-center gap-2">
                      <span className="font-mono font-bold text-accent">Q{idx + 1}.</span>
                      <span className="text-zinc-300 font-medium">{q.subjectName}</span>
                      <span className="text-zinc-500 font-mono">({q.topic})</span>
                    </div>
                    <Badge variant={q.difficulty === 'hard' ? 'red' : q.difficulty === 'medium' ? 'yellow' : 'green'}>
                      {q.difficulty}
                    </Badge>
                  </div>

                  <p className="text-sm text-white font-medium leading-relaxed">{q.stem}</p>

                  {/* Options */}
                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
                    {q.options.map((opt) => {
                      const isOptionSelected = selectedAns === opt;
                      let optionStyle = 'border-zinc-800 bg-zinc-900 text-zinc-300 hover:border-zinc-700';

                      if (paperSubmitted) {
                        if (opt === q.correctAnswer) {
                          optionStyle = 'border-emerald-500/60 bg-emerald-500/20 text-emerald-200 font-bold';
                        } else if (isOptionSelected) {
                          optionStyle = 'border-rose-500/60 bg-rose-500/20 text-rose-200';
                        }
                      } else if (isOptionSelected) {
                        optionStyle = 'border-accent bg-accent/20 text-white font-bold';
                      }

                      return (
                        <button
                          key={opt}
                          disabled={paperSubmitted}
                          onClick={() => {
                            setPaperUserAnswers((prev) => ({
                              ...prev,
                              [q.id]: opt,
                            }));
                          }}
                          className={`p-2.5 rounded-lg border text-left text-xs transition flex items-center justify-between ${optionStyle}`}
                        >
                          <span>{opt}</span>
                          {paperSubmitted && opt === q.correctAnswer && (
                            <Check className="w-3.5 h-3.5 text-emerald-400 shrink-0" />
                          )}
                        </button>
                      );
                    })}
                  </div>

                  {/* Solution display if submitted */}
                  {paperSubmitted && q.solutionMd && (
                    <div className="p-3 rounded-lg bg-zinc-950 border border-zinc-800/80 text-xs text-zinc-400 space-y-1">
                      <span className="text-accent font-semibold">Solution:</span>
                      <p className="leading-relaxed">{q.solutionMd}</p>
                    </div>
                  )}
                </div>
              );
            })}
          </div>

          {/* Paper Action Footer */}
          <div className="flex items-center justify-between pt-2 border-t border-zinc-800">
            <span className="text-xs text-zinc-400">
              Attempted: {Object.keys(paperUserAnswers).length} / {activePaper.totalQuestions}
            </span>
            {!paperSubmitted ? (
              <Button
                onClick={() => setPaperSubmitted(true)}
                size="sm"
                className="bg-emerald-600 hover:bg-emerald-500 text-white font-bold"
              >
                Submit & Grade Paper
              </Button>
            ) : (
              <Button
                onClick={() => {
                  setPaperSubmitted(false);
                  setPaperUserAnswers({});
                }}
                size="sm"
                variant="secondary"
              >
                Reset & Retake
              </Button>
            )}
          </div>
        </Card>
      )}

      {/* SECTION 1: Subject Weightage Breakdown & Target Hours vs Actual Score */}
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <div>
            <h3 className="text-base font-bold text-white flex items-center gap-2">
              <BarChart3 className="w-4 h-4 text-accent" />
              GATE CS Subject Marks Weightage & Study Allocation
            </h3>
            <p className="text-xs text-zinc-400">
              Historical marks breakdown vs your target & actual study hours
            </p>
          </div>
        </div>

        {overview && (
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            {overview.subjects.map((subj) => {
              const isExpanded = expandedSubject === subj.subjectKey;
              const hoursProgress = Math.min(100, Math.round((subj.actualHours / subj.targetHours) * 100));

              return (
                <div
                  key={subj.subjectKey}
                  className="p-4 rounded-xl border border-surface-border bg-surface-card/60 hover:border-zinc-700 transition space-y-3"
                >
                  <div className="flex items-start justify-between">
                    <div className="flex items-center gap-2.5">
                      <div
                        className="w-3 h-3 rounded-full shrink-0"
                        style={{ backgroundColor: subj.color }}
                      />
                      <div>
                        <h4 className="text-sm font-bold text-white leading-tight">
                          {subj.subjectName}
                        </h4>
                        <span className="text-[11px] text-zinc-400 font-mono">
                          GATE Weightage: <strong className="text-zinc-200">{subj.weightagePercent}%</strong> (~{subj.weightagePercent} Marks)
                        </span>
                      </div>
                    </div>
                    <Badge
                      variant={
                        subj.status === 'Mastered'
                          ? 'green'
                          : subj.status === 'On Track'
                          ? 'purple'
                          : subj.status === 'Needs Practice'
                          ? 'yellow'
                          : 'zinc'
                      }
                    >
                      {subj.status}
                    </Badge>
                  </div>

                  {/* Target Hours vs Actual Hours Progress Bar */}
                  <div className="space-y-1">
                    <div className="flex items-center justify-between text-xs">
                      <span className="text-zinc-400 flex items-center gap-1">
                        <Clock className="w-3 h-3 text-zinc-500" />
                        Target: {subj.targetHours}h
                      </span>
                      <span className="font-mono text-zinc-300">
                        Actual: <strong className="text-accent">{subj.actualHours}h</strong> ({hoursProgress}%)
                      </span>
                    </div>
                    <div className="w-full bg-zinc-800/80 h-2 rounded-full overflow-hidden">
                      <div
                        className="h-full rounded-full transition-all duration-500"
                        style={{
                          backgroundColor: subj.color,
                          width: `${hoursProgress}%`,
                        }}
                      />
                    </div>
                  </div>

                  {/* Accuracy & Mastery Metrics */}
                  <div className="grid grid-cols-3 gap-2 pt-1 text-center bg-zinc-900/60 p-2 rounded-lg border border-zinc-800/60">
                    <div>
                      <span className="text-[10px] text-zinc-500 uppercase font-semibold">Attempts</span>
                      <p className="text-xs font-bold text-white font-mono">{subj.attemptedCount}</p>
                    </div>
                    <div>
                      <span className="text-[10px] text-zinc-500 uppercase font-semibold">Accuracy</span>
                      <p className="text-xs font-bold text-emerald-400 font-mono">{subj.accuracy}%</p>
                    </div>
                    <div>
                      <span className="text-[10px] text-zinc-500 uppercase font-semibold">Mastery</span>
                      <p className="text-xs font-bold text-purple-400 font-mono">{subj.masteryPercentage}%</p>
                    </div>
                  </div>

                  {/* High Yield Subtopic Highlights */}
                  <div className="flex flex-wrap gap-1.5 pt-1">
                    {subj.highYieldSummary.map((item) => (
                      <span
                        key={item}
                        className="text-[10px] px-2 py-0.5 rounded-md bg-zinc-800/80 text-zinc-300 font-medium border border-zinc-700/50"
                      >
                        {item}
                      </span>
                    ))}
                  </div>

                  {/* Quick Expand to see details */}
                  <button
                    onClick={() => setExpandedSubject(isExpanded ? null : subj.subjectKey)}
                    className="w-full pt-1 text-[11px] text-accent hover:text-accent/80 font-medium flex items-center justify-center gap-1 transition"
                  >
                    <span>{isExpanded ? 'Hide Topic Details' : 'View High-Yield Topics'}</span>
                    {isExpanded ? <ChevronUp className="w-3.5 h-3.5" /> : <ChevronDown className="w-3.5 h-3.5" />}
                  </button>

                  {/* Expanded Subtopics view */}
                  {isExpanded && (
                    <div className="pt-2 border-t border-zinc-800 space-y-2 text-xs">
                      {highYieldTopics
                        .filter((t) => t.subjectKey === subj.subjectKey)
                        .map((topic) => (
                          <div
                            key={topic.id}
                            className="p-2 rounded-lg bg-zinc-950/70 border border-zinc-800 flex items-center justify-between gap-2"
                          >
                            <div className="min-w-0 flex-1">
                              <div className="flex items-center gap-1.5">
                                <span className="font-semibold text-white truncate">{topic.topicName}</span>
                                <Badge
                                  size="sm"
                                  variant={topic.priority === 'Critical' ? 'red' : topic.priority === 'High' ? 'yellow' : 'blue'}
                                >
                                  {topic.priority}
                                </Badge>
                              </div>
                              <span className="text-[10px] text-zinc-500 block truncate">
                                {topic.keyFormulaOrConcept}
                              </span>
                            </div>
                            <button
                              onClick={() => handleToggleChecklist(topic.id, topic.isCompleted)}
                              className="shrink-0 p-1 text-zinc-400 hover:text-accent transition"
                            >
                              {topic.isCompleted ? (
                                <CheckCircle2 className="w-4 h-4 text-emerald-400" />
                              ) : (
                                <Circle className="w-4 h-4 text-zinc-600" />
                              )}
                            </button>
                          </div>
                        ))}
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        )}
      </div>

      {/* SECTION 2: High-Yield Topic Checklist */}
      <div className="space-y-4 pt-2">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
          <div>
            <h3 className="text-base font-bold text-white flex items-center gap-2">
              <CheckSquare className="w-4 h-4 text-purple-400" />
              High-Yield Subtopics Checklist
            </h3>
            <p className="text-xs text-zinc-400">
              Proven topics with &gt;80% recurring frequency in past 10 years of GATE CS PYQs
            </p>
          </div>

          <div className="flex items-center gap-1.5 text-xs">
            <span className="text-zinc-500">Filter status:</span>
            <button
              onClick={() => setStatusFilter('all')}
              className={`px-2 py-0.5 rounded-lg border text-xs transition ${
                statusFilter === 'all'
                  ? 'bg-accent/20 border-accent text-white font-semibold'
                  : 'bg-zinc-900 border-zinc-800 text-zinc-400 hover:text-white'
              }`}
            >
              All
            </button>
            <button
              onClick={() => setStatusFilter('pending')}
              className={`px-2 py-0.5 rounded-lg border text-xs transition ${
                statusFilter === 'pending'
                  ? 'bg-accent/20 border-accent text-white font-semibold'
                  : 'bg-zinc-900 border-zinc-800 text-zinc-400 hover:text-white'
              }`}
            >
              Pending
            </button>
            <button
              onClick={() => setStatusFilter('completed')}
              className={`px-2 py-0.5 rounded-lg border text-xs transition ${
                statusFilter === 'completed'
                  ? 'bg-accent/20 border-accent text-white font-semibold'
                  : 'bg-zinc-900 border-zinc-800 text-zinc-400 hover:text-white'
              }`}
            >
              Mastered
            </button>
          </div>
        </div>

        {/* Search & Subject Chips */}
        <div className="space-y-2.5">
          <div className="relative">
            <Search className="w-4 h-4 text-zinc-500 absolute left-3.5 top-1/2 -translate-y-1/2" />
            <input
              type="text"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              placeholder="Search high-yield subtopic, formula, or concept (e.g. Cache, Paging, Dijkstra, BCNF)..."
              className="w-full pl-9 pr-4 py-2 rounded-xl bg-surface-card/80 border border-surface-border text-white text-xs placeholder-zinc-500 focus:outline-none focus:border-accent"
            />
          </div>

          {/* Subject Filter Chips */}
          <div className="flex items-center gap-1.5 overflow-x-auto pb-1 no-scrollbar text-xs">
            <button
              onClick={() => setSelectedSubjectFilter('all')}
              className={`px-2.5 py-1 rounded-lg border whitespace-nowrap transition ${
                selectedSubjectFilter === 'all'
                  ? 'bg-accent/20 border-accent text-white font-bold'
                  : 'bg-surface-card/60 border-surface-border text-zinc-400 hover:text-zinc-200'
              }`}
            >
              All Subjects ({highYieldTopics.length})
            </button>
            {overview?.subjects.map((subj) => (
              <button
                key={subj.subjectKey}
                onClick={() => setSelectedSubjectFilter(subj.subjectKey)}
                className={`px-2.5 py-1 rounded-lg border whitespace-nowrap transition flex items-center gap-1.5 ${
                  selectedSubjectFilter === subj.subjectKey
                    ? 'bg-accent/20 border-accent text-white font-bold'
                    : 'bg-surface-card/60 border-surface-border text-zinc-400 hover:text-zinc-200'
                }`}
              >
                <span className="w-2 h-2 rounded-full" style={{ backgroundColor: subj.color }} />
                <span>{subj.subjectName}</span>
              </button>
            ))}
          </div>

          {/* Priority Filter */}
          <div className="flex items-center gap-2 text-xs">
            <span className="text-zinc-500 text-[11px]">Priority:</span>
            {['all', 'Critical', 'High', 'Medium'].map((prio) => (
              <button
                key={prio}
                onClick={() => setSelectedPriorityFilter(prio)}
                className={`px-2 py-0.5 rounded-md border text-[11px] transition ${
                  selectedPriorityFilter === prio
                    ? 'bg-zinc-800 border-zinc-600 text-white font-semibold'
                    : 'bg-zinc-900/60 border-zinc-800 text-zinc-500 hover:text-zinc-300'
                }`}
              >
                {prio === 'all' ? 'All Priorities' : prio}
              </button>
            ))}
          </div>
        </div>

        {/* Checklist Items Grid */}
        <div className="space-y-2.5">
          {filteredTopics.length === 0 ? (
            <div className="p-8 text-center text-xs text-zinc-500 border border-dashed border-zinc-800 rounded-xl">
              No high-yield topics match your active search filters.
            </div>
          ) : (
            filteredTopics.map((topic) => (
              <div
                key={topic.id}
                className={`p-3.5 rounded-xl border transition flex items-start gap-3.5 ${
                  topic.isCompleted
                    ? 'bg-emerald-950/10 border-emerald-900/30'
                    : 'bg-surface-card/60 border-surface-border hover:border-zinc-700'
                }`}
              >
                {/* Checkbox */}
                <button
                  onClick={() => handleToggleChecklist(topic.id, topic.isCompleted)}
                  className="mt-0.5 shrink-0 text-zinc-500 hover:text-accent transition"
                  title={topic.isCompleted ? 'Mark as Pending' : 'Mark as Mastered'}
                >
                  {topic.isCompleted ? (
                    <CheckCircle2 className="w-5 h-5 text-emerald-400" />
                  ) : (
                    <Circle className="w-5 h-5 text-zinc-600 hover:text-zinc-400" />
                  )}
                </button>

                {/* Topic Info */}
                <div className="min-w-0 flex-1 space-y-1.5">
                  <div className="flex flex-wrap items-center gap-2">
                    <h4
                      className={`text-sm font-bold ${
                        topic.isCompleted ? 'text-zinc-400 line-through' : 'text-white'
                      }`}
                    >
                      {topic.topicName}
                    </h4>

                    <Badge
                      variant={
                        topic.priority === 'Critical'
                          ? 'red'
                          : topic.priority === 'High'
                          ? 'yellow'
                          : 'blue'
                      }
                    >
                      {topic.priority}
                    </Badge>

                    <span className="text-[11px] font-mono text-zinc-400 flex items-center gap-1">
                      <Zap className="w-3 h-3 text-amber-400" />
                      {topic.frequencyScore}% PYQs
                    </span>

                    <span className="text-[11px] font-mono text-purple-300">
                      ~{topic.avgMarks} Marks
                    </span>
                  </div>

                  <p className="text-xs text-zinc-400 leading-relaxed">
                    {topic.recurrencePattern}
                  </p>

                  {/* Formula / Key Insight pill */}
                  <div className="p-2 rounded-lg bg-zinc-950/80 border border-zinc-800/80 font-mono text-[11px] text-zinc-300 leading-relaxed overflow-x-auto">
                    <span className="text-accent font-semibold mr-1.5">Key Insight:</span>
                    <span>{topic.keyFormulaOrConcept}</span>
                  </div>
                </div>
              </div>
            ))
          )}
        </div>
      </div>

      {/* SECTION 3: Recent Weighted Papers */}
      {recentPapers.length > 0 && (
        <div className="space-y-3 pt-2">
          <h3 className="text-base font-bold text-white flex items-center gap-2">
            <FileText className="w-4 h-4 text-accent" />
            Generated Weighted Papers History
          </h3>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            {recentPapers.map((p) => (
              <div
                key={p.paperId}
                className="p-3.5 rounded-xl border border-surface-border bg-surface-card/60 flex items-center justify-between gap-3 hover:border-zinc-700 transition"
              >
                <div className="min-w-0 flex-1">
                  <h4 className="text-sm font-bold text-white truncate">{p.title}</h4>
                  <div className="flex items-center gap-3 text-xs text-zinc-400 mt-1 font-mono">
                    <span>{p.totalQuestions} Questions</span>
                    <span>{p.durationMinutes} mins</span>
                    <span>{new Date(p.createdAt).toLocaleDateString()}</span>
                  </div>
                </div>

                <Button
                  onClick={() => handleViewPaper(p.paperId)}
                  size="sm"
                  variant="secondary"
                  className="shrink-0 text-xs"
                >
                  Open
                </Button>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* MODAL: Generate Weighted Paper */}
      <Modal
        isOpen={isGenerateModalOpen}
        onClose={() => setIsGenerateModalOpen(false)}
        title="Generate Weighted GATE CS Paper"
      >
        <div className="space-y-5">
          <p className="text-xs text-zinc-400 leading-relaxed">
            Sensei automatically samples questions strictly proportional to official GATE CS historical marks weightage (Engg Math 13%, Aptitude 15%, OS 10%, DBMS 8%, CN 9%, Algorithms 11%, TOC/CD 12%, COA/Digital 12%).
          </p>

          {/* Preset Buttons */}
          <div className="space-y-2">
            <label className="text-xs font-semibold text-zinc-300">Select Question Preset:</label>
            <div className="grid grid-cols-2 gap-2">
              {[
                { count: 10, label: '10 Qs (Sprint)', time: '15 mins' },
                { count: 25, label: '25 Qs (Targeted)', time: '45 mins' },
                { count: 30, label: '30 Qs (Standard)', time: '60 mins' },
                { count: 65, label: '65 Qs (Full Mock)', time: '180 mins' },
              ].map((preset) => (
                <button
                  key={preset.count}
                  type="button"
                  onClick={() => setQuestionCountPreset(preset.count)}
                  className={`p-3 rounded-xl border text-left transition flex flex-col justify-between ${
                    questionCountPreset === preset.count
                      ? 'border-accent bg-accent/20 text-white shadow-lg shadow-accent/10'
                      : 'border-zinc-800 bg-zinc-900/60 text-zinc-400 hover:border-zinc-700'
                  }`}
                >
                  <span className="font-bold text-xs text-white">{preset.label}</span>
                  <span className="text-[10px] text-zinc-500 font-mono mt-1">{preset.time}</span>
                </button>
              ))}
            </div>
          </div>

          {/* Optional Title */}
          <div className="space-y-1.5">
            <label className="text-xs font-semibold text-zinc-300">Custom Paper Title (optional):</label>
            <input
              type="text"
              value={paperTitle}
              onChange={(e) => setPaperTitle(e.target.value)}
              placeholder={`e.g. GATE CS Weighted Mock #${recentPapers.length + 1}`}
              className="w-full px-3.5 py-2 rounded-xl bg-zinc-900 border border-zinc-800 text-white text-xs placeholder-zinc-600 focus:outline-none focus:border-accent"
            />
          </div>

          {/* Expected Question Allocation Preview */}
          <div className="p-3.5 rounded-xl border border-zinc-800 bg-zinc-950 space-y-2">
            <span className="text-xs font-bold text-zinc-300">Weightage Distribution Breakdown:</span>
            <div className="grid grid-cols-2 gap-1.5 text-[11px] font-mono">
              <span className="text-lime-400">General Aptitude (15%): ~{Math.max(1, Math.round(questionCountPreset * 0.15))} Qs</span>
              <span className="text-purple-400">Engg & Discrete Math (13%): ~{Math.max(1, Math.round(questionCountPreset * 0.13))} Qs</span>
              <span className="text-cyan-400">TOC & Compiler Design (12%): ~{Math.max(1, Math.round(questionCountPreset * 0.12))} Qs</span>
              <span className="text-rose-400">COA & Digital Logic (12%): ~{Math.max(1, Math.round(questionCountPreset * 0.12))} Qs</span>
              <span className="text-violet-400">Algorithms & DS (11%): ~{Math.max(1, Math.round(questionCountPreset * 0.11))} Qs</span>
              <span className="text-pink-400">Operating Systems (10%): ~{Math.max(1, Math.round(questionCountPreset * 0.10))} Qs</span>
              <span className="text-amber-400">Computer Networks (9%): ~{Math.max(1, Math.round(questionCountPreset * 0.09))} Qs</span>
              <span className="text-emerald-400">DBMS (8%): ~{Math.max(1, Math.round(questionCountPreset * 0.08))} Qs</span>
            </div>
          </div>

          {/* Submit Button */}
          <div className="flex items-center justify-end gap-2 pt-2">
            <Button
              type="button"
              variant="secondary"
              size="sm"
              onClick={() => setIsGenerateModalOpen(false)}
            >
              Cancel
            </Button>
            <Button
              type="button"
              size="sm"
              onClick={handleGeneratePaper}
              disabled={generating}
              className="bg-accent font-bold"
            >
              {generating ? (
                <>
                  <RefreshCw className="w-3.5 h-3.5 animate-spin mr-1.5" />
                  Generating Paper...
                </>
              ) : (
                <>
                  <Sparkles className="w-3.5 h-3.5 mr-1.5" />
                  Generate Paper
                </>
              )}
            </Button>
          </div>
        </div>
      </Modal>
    </div>
  );
};
