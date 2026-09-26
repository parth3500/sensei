import React from 'react';
import { CheckSquare, BookOpen, Award, Video, FolderGit2, BarChart3 } from 'lucide-react';

export type TabType = 'today' | 'practice' | 'mock' | 'vault' | 'weightage' | 'flashcards' | 'lectures' | 'drive' | 'progress' | 'ai' | 'modules' | 'settings';

interface BottomNavProps {
  currentTab: TabType;
  onSelectTab: (tab: TabType) => void;
  pendingDueCount?: number;
}

export const BottomNav: React.FC<BottomNavProps> = ({
  currentTab,
  onSelectTab,
  pendingDueCount = 0,
}) => {
  const tabs = [
    { id: 'today' as TabType, label: 'Today', icon: CheckSquare },
    { id: 'practice' as TabType, label: 'Practice', icon: BookOpen, badge: pendingDueCount > 0 ? pendingDueCount : null },
    { id: 'mock' as TabType, label: 'Mock Exam', icon: Award },
    { id: 'lectures' as TabType, label: 'Lectures', icon: Video },
    { id: 'drive' as TabType, label: 'Drive', icon: FolderGit2 },
    { id: 'progress' as TabType, label: 'Stats', icon: BarChart3 },
  ];

  return (
    <nav className="fixed bottom-0 left-0 right-0 z-40 bg-surface/90 backdrop-blur-xl border-t border-surface-border px-3 py-2 pb-[max(0.75rem,env(safe-area-inset-bottom))]">
      <div className="max-w-xl mx-auto flex items-center justify-around">
        {tabs.map((tab) => {
          const Icon = tab.icon;
          const isActive = currentTab === tab.id;
          return (
            <button
              key={tab.id}
              onClick={() => onSelectTab(tab.id)}
              className={`relative flex flex-col items-center gap-1 px-2.5 py-1.5 rounded-xl transition select-none ${
                isActive
                  ? 'text-accent font-semibold'
                  : 'text-zinc-500 hover:text-zinc-300'
              }`}
            >
              <div className="relative">
                <Icon className={`w-5 h-5 transition-transform ${isActive ? 'scale-110' : ''}`} />
                {tab.badge && (
                  <span className="absolute -top-1 -right-2.5 px-1.5 py-0.2 rounded-full bg-accent text-[9px] font-bold text-white leading-none">
                    {tab.badge}
                  </span>
                )}
              </div>
              <span className="text-[11px] leading-none">{tab.label}</span>
            </button>
          );
        })}
      </div>
    </nav>
  );
};
