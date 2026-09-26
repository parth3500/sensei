import React from 'react';
import { Flame, Calendar, Sparkles, Layers, BookMarked, Settings, BarChart3, BrainCircuit } from 'lucide-react';
import { StatsOverview } from '@/types';
import { TabType } from './BottomNav';

interface AppHeaderProps {
  stats: StatsOverview | null;
  currentTab: TabType;
  onSelectTab: (tab: TabType) => void;
}

export const AppHeader: React.FC<AppHeaderProps> = ({ stats, currentTab, onSelectTab }) => {
  return (
    <header className="sticky top-0 z-30 bg-surface/80 backdrop-blur-xl border-b border-surface-border px-4 py-3">
      <div className="max-w-2xl mx-auto flex items-center justify-between">
        <div
          onClick={() => onSelectTab('today')}
          className="flex items-center gap-2.5 cursor-pointer hover:opacity-90 transition"
        >
          <div className="w-8 h-8 rounded-xl bg-gradient-to-tr from-accent to-purple-400 flex items-center justify-center shadow-lg shadow-accent/20">
            <span className="text-white font-black text-sm">S</span>
          </div>
          <div>
            <h1 className="text-base font-bold text-white tracking-tight leading-none">Sensei</h1>
            <span className="text-[10px] text-zinc-400 font-medium">GATE 2027 Prep</span>
          </div>
        </div>

        <div className="flex items-center gap-2 sm:gap-3">
          {stats && (
            <>
              <div className="hidden xs:flex items-center gap-1.5 px-2.5 py-1 rounded-xl bg-amber-500/10 border border-amber-500/20 text-amber-300 text-xs font-semibold">
                <Flame className="w-3.5 h-3.5 fill-amber-400 text-amber-400 animate-pulse" />
                <span>{stats.streak}d</span>
              </div>
              <div className="hidden sm:flex items-center gap-1.5 px-2.5 py-1 rounded-xl bg-purple-500/10 border border-purple-500/20 text-purple-300 text-xs font-semibold">
                <Calendar className="w-3.5 h-3.5 text-purple-400" />
                <span>{stats.examCountdownDays}d left</span>
              </div>
            </>
          )}

          {/* Flashcards & Leitner Spaced Repetition Button */}
          <button
            onClick={() => onSelectTab('flashcards')}
            className={`p-1.5 rounded-xl border transition ${
              currentTab === 'flashcards'
                ? 'bg-accent/20 border-accent text-white'
                : 'bg-zinc-800/80 border-surface-border text-zinc-400 hover:text-white'
            }`}
            title="Flashcards & Leitner Spaced Repetition"
          >
            <BrainCircuit className="w-4 h-4" />
          </button>


          {/* GATE CS Weightage & Syllabus Analytics Button */}
          <button
            onClick={() => onSelectTab('weightage')}
            className={`p-1.5 rounded-xl border transition ${
              currentTab === 'weightage'
                ? 'bg-accent/20 border-accent text-white'
                : 'bg-zinc-800/80 border-surface-border text-zinc-400 hover:text-white'
            }`}
            title="GATE Weightage & Syllabus Analytics"
          >
            <BarChart3 className="w-4 h-4" />
          </button>

          {/* Formula & Theorem Vault Button */}
          <button
            onClick={() => onSelectTab('vault')}
            className={`p-1.5 rounded-xl border transition ${
              currentTab === 'vault'
                ? 'bg-accent/20 border-accent text-white'
                : 'bg-zinc-800/80 border-surface-border text-zinc-400 hover:text-white'
            }`}
            title="Formula & Theorem Vault"
          >
            <BookMarked className="w-4 h-4" />
          </button>

          {/* Pluggable Modules Hub Button */}
          <button
            onClick={() => onSelectTab('modules')}
            className={`p-1.5 rounded-xl border transition ${
              currentTab === 'modules'
                ? 'bg-accent/20 border-accent text-white'
                : 'bg-zinc-800/80 border-surface-border text-zinc-400 hover:text-white'
            }`}
            title="Component Modules Hub"
          >
            <Layers className="w-4 h-4" />
          </button>

          {/* AI Tutor Button */}
          <button
            onClick={() => onSelectTab('ai')}
            className={`p-1.5 rounded-xl border transition ${
              currentTab === 'ai'
                ? 'bg-accent/20 border-accent text-accent'
                : 'bg-zinc-800/80 border-surface-border text-accent hover:text-white'
            }`}
            title="Ask Sensei AI"
          >
            <Sparkles className="w-4 h-4" />
          </button>

          {/* Settings Button */}
          <button
            onClick={() => onSelectTab('settings')}
            className={`p-1.5 rounded-xl border transition ${
              currentTab === 'settings'
                ? 'bg-accent/20 border-accent text-white'
                : 'bg-zinc-800/80 border-surface-border text-zinc-400 hover:text-white'
            }`}
            title="Settings & System"
          >
            <Settings className="w-4 h-4" />
          </button>
        </div>
      </div>
    </header>
  );
};
