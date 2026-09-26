import React from 'react';
import {
  Flame,
  Target,
  Calendar,
  CheckSquare,
  AlertTriangle,
  TrendingUp,
  BrainCircuit,
  Clock,
  BookOpen,
  ShieldCheck,
  ChevronRight
} from 'lucide-react';
import { StatsOverview, HeatmapItem, WeakTopicItem, AdvancedAnalytics } from '@/types';
import { Card } from '@/components/ui/Card';
import { Badge } from '@/components/ui/Badge';
import { Button } from '@/components/ui/Button';

interface ProgressViewProps {
  stats: StatsOverview | null;
  heatmap: HeatmapItem[];
  weakTopics: WeakTopicItem[];
  advancedAnalytics?: AdvancedAnalytics | null;
  onGoToRecall?: () => void;
}

export const ProgressView: React.FC<ProgressViewProps> = ({
  stats,
  heatmap,
  weakTopics,
  advancedAnalytics,
  onGoToRecall,
}) => {
  const retentionIndex = advancedAnalytics?.retentionRateIndex ?? 85;
  const subjectMastery = advancedAnalytics?.subjectMastery ?? [
    { subject: 'Algorithms & Data Structures', totalQuestions: 15, mastered: 12, masteryPercentage: 80 },
    { subject: 'Operating Systems', totalQuestions: 12, mastered: 8, masteryPercentage: 66 },
    { subject: 'Computer Networks', totalQuestions: 10, mastered: 7, masteryPercentage: 70 },
    { subject: 'DBMS', totalQuestions: 8, mastered: 6, masteryPercentage: 75 },
  ];
  const timeSpent = advancedAnalytics?.timeSpent ?? [
    { category: 'Video Lectures', minutes: 240 },
    { category: 'Spaced Repetition & Recall', minutes: 120 },
    { category: 'Drive Notes Revision', minutes: 75 },
  ];
  const highRiskTopics = advancedAnalytics?.highRiskTopics ?? [];

  const totalTimeMin = timeSpent.reduce((acc, t) => acc + t.minutes, 0);

  return (
    <div className="space-y-6 pb-20">
      {/* Header */}
      <div>
        <h2 className="text-2xl font-extrabold text-white tracking-tight">Analytics &amp; Mastery</h2>
        <p className="text-xs text-zinc-400">Track your trajectory toward GATE 2027</p>
      </div>

      {/* Core Metric Cards Grid */}
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
        <Card className="text-center p-4">
          <div className="flex items-center justify-center gap-1.5 text-xs text-amber-400 font-semibold mb-1">
            <Flame className="w-4 h-4 fill-amber-400" />
            Study Streak
          </div>
          <p className="text-3xl font-black text-white">{stats?.streak ?? 0}</p>
          <span className="text-[11px] text-zinc-500">consecutive days</span>
        </Card>

        <Card className="text-center p-4">
          <div className="flex items-center justify-center gap-1.5 text-xs text-emerald-400 font-semibold mb-1">
            <Target className="w-4 h-4" />
            Accuracy
          </div>
          <p className="text-3xl font-black text-white">{stats?.accuracy ?? 0}%</p>
          <span className="text-[11px] text-zinc-500">PYQ attempts</span>
        </Card>

        <Card className="text-center p-4">
          <div className="flex items-center justify-center gap-1.5 text-xs text-purple-400 font-semibold mb-1">
            <Calendar className="w-4 h-4" />
            Days Left
          </div>
          <p className="text-3xl font-black text-white">{stats?.examCountdownDays ?? 0}</p>
          <span className="text-[11px] text-zinc-500">to GATE 2027</span>
        </Card>

        <Card className="text-center p-4">
          <div className="flex items-center justify-center gap-1.5 text-xs text-blue-400 font-semibold mb-1">
            <CheckSquare className="w-4 h-4" />
            Today&apos;s Focus
          </div>
          <p className="text-3xl font-black text-white">
            {stats?.todayTasksDone ?? 0}/{stats?.todayTasksTotal ?? 0}
          </p>
          <span className="text-[11px] text-zinc-500">tasks done</span>
        </Card>
      </div>

      {/* Memory Retention & Ebbinghaus Decay Overview */}
      <Card className="space-y-4 bg-gradient-to-br from-surface-card via-surface-card to-purple-950/20 border-accent/20">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2.5">
            <div className="w-9 h-9 rounded-xl bg-accent/20 text-accent border border-accent/30 flex items-center justify-center">
              <BrainCircuit className="w-5 h-5" />
            </div>
            <div>
              <h3 className="text-sm font-bold text-white">Memory Retention Index</h3>
              <p className="text-xs text-zinc-400">Ebbinghaus forgetting curve modeling</p>
            </div>
          </div>
          <div className="text-right">
            <span className="text-2xl font-black text-white">{retentionIndex}%</span>
            <p className="text-[10px] text-emerald-400 font-medium">Optimal stability</p>
          </div>
        </div>

        {/* Retention Progress Bar */}
        <div className="w-full bg-zinc-800/80 rounded-full h-2.5 overflow-hidden">
          <div
            className="h-full rounded-full bg-gradient-to-r from-accent to-emerald-400 transition-all duration-500"
            style={{ width: `${Math.min(100, Math.max(0, retentionIndex))}%` }}
          />
        </div>

        {highRiskTopics.length > 0 && (
          <div className="p-3 rounded-xl bg-rose-500/10 border border-rose-500/20 flex items-center justify-between">
            <div className="space-y-0.5">
              <p className="text-xs font-semibold text-rose-300">
                {highRiskTopics.length} topic{highRiskTopics.length > 1 ? 's' : ''} fading from memory
              </p>
              <p className="text-[11px] text-zinc-400">
                Review now before forgetting accelerates.
              </p>
            </div>
            {onGoToRecall && (
              <Button onClick={onGoToRecall} size="sm" variant="danger">
                Review at-risk
                <ChevronRight className="w-3.5 h-3.5" />
              </Button>
            )}
          </div>
        )}
      </Card>

      {/* Subject Mastery Progress Bars */}
      <Card className="space-y-4">
        <div className="flex items-center justify-between">
          <h3 className="text-sm font-bold text-white flex items-center gap-2">
            <ShieldCheck className="w-4 h-4 text-emerald-400" />
            Subject Syllabus Mastery
          </h3>
          <span className="text-xs text-zinc-500">{subjectMastery.length} subjects</span>
        </div>

        <div className="space-y-3.5">
          {subjectMastery.map((sub, idx) => (
            <div key={idx} className="space-y-1.5">
              <div className="flex items-center justify-between text-xs">
                <span className="font-semibold text-zinc-200">{sub.subject}</span>
                <span className="text-zinc-400 font-mono">
                  {sub.mastered}/{sub.totalQuestions} ({sub.masteryPercentage}%)
                </span>
              </div>
              <div className="w-full bg-zinc-800 rounded-full h-2 overflow-hidden">
                <div
                  className={`h-full rounded-full transition-all duration-300 ${
                    sub.masteryPercentage >= 75
                      ? 'bg-emerald-500'
                      : sub.masteryPercentage >= 50
                      ? 'bg-accent'
                      : 'bg-amber-500'
                  }`}
                  style={{ width: `${sub.masteryPercentage}%` }}
                />
              </div>
            </div>
          ))}
        </div>
      </Card>

      {/* Time Allocation Breakdown */}
      <Card className="space-y-4">
        <div className="flex items-center justify-between">
          <h3 className="text-sm font-bold text-white flex items-center gap-2">
            <Clock className="w-4 h-4 text-blue-400" />
            Time Allocation Breakdown
          </h3>
          <span className="text-xs text-zinc-500">{Math.round(totalTimeMin / 60)} hrs total</span>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
          {timeSpent.map((t, idx) => {
            const pct = totalTimeMin > 0 ? Math.round((t.minutes / totalTimeMin) * 100) : 0;
            return (
              <div key={idx} className="p-3 rounded-xl bg-zinc-900/60 border border-surface-border space-y-1">
                <span className="text-xs text-zinc-400 font-medium">{t.category}</span>
                <div className="flex items-baseline justify-between">
                  <p className="text-lg font-bold text-white">{t.minutes}m</p>
                  <span className="text-xs font-mono text-accent">{pct}%</span>
                </div>
              </div>
            );
          })}
        </div>
      </Card>

      {/* Activity Consistency Grid (Heatmap) */}
      <Card className="space-y-3">
        <div className="flex items-center justify-between">
          <h3 className="text-sm font-bold text-white flex items-center gap-2">
            <TrendingUp className="w-4 h-4 text-accent" />
            Study Consistency (Recent Activity)
          </h3>
          <span className="text-xs text-zinc-500">{heatmap.length} active sessions</span>
        </div>

        {heatmap.length === 0 ? (
          <p className="text-xs text-zinc-500 py-4 text-center">Complete daily tasks to light up your activity heatmap.</p>
        ) : (
          <div className="flex flex-wrap gap-1.5 py-2">
            {heatmap.slice(-60).map((item, idx) => (
              <div
                key={idx}
                title={`${item.date}: ${item.count} tasks completed`}
                className={`w-4 h-4 rounded-md transition hover:scale-125 cursor-pointer ${
                  item.count >= 4
                    ? 'bg-accent shadow-sm shadow-accent/50'
                    : item.count >= 2
                    ? 'bg-accent/70'
                    : 'bg-accent/40'
                }`}
              />
            ))}
          </div>
        )}
      </Card>

      {/* Weak Topics Drilldown */}
      <Card className="space-y-4">
        <div className="flex items-center justify-between">
          <h3 className="text-sm font-bold text-white flex items-center gap-2">
            <AlertTriangle className="w-4 h-4 text-amber-400" />
            Identified Weak Topics
          </h3>
          <span className="text-[11px] text-zinc-500">Based on error rate</span>
        </div>

        {weakTopics.length === 0 ? (
          <p className="text-xs text-zinc-500 py-2">Practice more questions to identify specific weak areas.</p>
        ) : (
          <div className="space-y-2.5">
            {weakTopics.map((topic, idx) => (
              <div
                key={idx}
                className="flex items-center justify-between p-3 rounded-xl bg-zinc-900/60 border border-surface-border text-xs"
              >
                <div className="space-y-1">
                  <p className="font-semibold text-zinc-200">{topic.topic}</p>
                  <p className="text-[10px] text-zinc-500">
                    {topic.totalAttempts} attempts &bull; {topic.mistakes} mistakes
                  </p>
                </div>
                <div className="text-right space-y-1">
                  <Badge variant={topic.accuracy < 50 ? 'red' : topic.accuracy < 75 ? 'yellow' : 'green'}>
                    {topic.accuracy}% acc
                  </Badge>
                </div>
              </div>
            ))}
          </div>
        )}
      </Card>
    </div>
  );
};
