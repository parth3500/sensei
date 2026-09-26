import React, { useState } from 'react';
import { Plus, Check, Trash2, Clock, Calendar, CheckCircle2 } from 'lucide-react';
import confetti from 'canvas-confetti';
import { TaskItem } from '@/types';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';
import { Badge } from '@/components/ui/Badge';
import { Modal } from '@/components/ui/Modal';

interface TodayViewProps {
  tasks: TaskItem[];
  onToggleTask: (task: TaskItem) => Promise<void>;
  onAddTask: (task: { title: string; priority: string; estMin?: number; dueDate?: string }) => Promise<void>;
  onDeleteTask: (id: number) => Promise<void>;
}

export const TodayView: React.FC<TodayViewProps> = ({
  tasks,
  onToggleTask,
  onAddTask,
  onDeleteTask,
}) => {
  const [filter, setFilter] = useState<'all' | 'pending' | 'done'>('all');
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [title, setTitle] = useState('');
  const [priority, setPriority] = useState('medium');
  const [estMin, setEstMin] = useState<number>(30);
  const [loading, setLoading] = useState(false);

  const doneCount = tasks.filter((t) => t.status === 'done').length;
  const progressPercent = tasks.length > 0 ? Math.round((doneCount / tasks.length) * 100) : 0;

  const filteredTasks = tasks.filter((t) => {
    if (filter === 'pending') return t.status === 'pending';
    if (filter === 'done') return t.status === 'done';
    return true;
  });

  const handleToggle = async (task: TaskItem) => {
    const willBeDone = task.status === 'pending';
    await onToggleTask(task);
    if (willBeDone) {
      confetti({
        particleCount: 80,
        spread: 60,
        origin: { y: 0.7 },
        colors: ['#7c5cff', '#10b981', '#f59e0b', '#ec4899'],
      });
    }
  };

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!title.trim()) return;

    setLoading(true);
    try {
      await onAddTask({
        title: title.trim(),
        priority,
        estMin: estMin > 0 ? estMin : undefined,
      });
      setTitle('');
      setPriority('medium');
      setEstMin(30);
      setIsModalOpen(false);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="space-y-6 pb-20">
      {/* Header & Quick Add */}
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-extrabold text-white tracking-tight">Today&apos;s Focus</h2>
          <p className="text-xs text-zinc-400">
            {new Date().toLocaleDateString('en-IN', { weekday: 'long', month: 'short', day: 'numeric' })}
          </p>
        </div>
        <Button onClick={() => setIsModalOpen(true)} size="sm">
          <Plus className="w-4 h-4" />
          Add Task
        </Button>
      </div>

      {/* Progress Bar Card */}
      <Card className="space-y-3">
        <div className="flex justify-between items-center text-xs">
          <span className="text-zinc-400 font-medium">Daily Completion</span>
          <span className="text-accent font-bold">
            {doneCount} / {tasks.length} tasks ({progressPercent}%)
          </span>
        </div>
        <div className="w-full bg-zinc-800/80 h-2.5 rounded-full overflow-hidden p-0.5 border border-zinc-700/50">
          <div
            className="bg-gradient-to-r from-accent to-purple-400 h-full rounded-full transition-all duration-500 ease-out"
            style={{ width: `${progressPercent}%` }}
          />
        </div>
      </Card>

      {/* Filter Tabs */}
      <div className="flex items-center gap-1.5 p-1 bg-zinc-900/80 border border-surface-border rounded-xl w-fit">
        {(['all', 'pending', 'done'] as const).map((tab) => (
          <button
            key={tab}
            onClick={() => setFilter(tab)}
            className={`px-3 py-1 rounded-lg text-xs font-semibold capitalize transition ${
              filter === tab
                ? 'bg-accent text-white shadow-sm'
                : 'text-zinc-400 hover:text-zinc-200'
            }`}
          >
            {tab}
          </button>
        ))}
      </div>

      {/* Task List */}
      <div className="space-y-2.5">
        {filteredTasks.length === 0 ? (
          <div className="p-10 text-center rounded-2xl border border-dashed border-zinc-800 bg-surface-card/30 space-y-2">
            <CheckCircle2 className="w-8 h-8 text-zinc-600 mx-auto" />
            <p className="text-sm text-zinc-400 font-medium">
              {filter === 'done' ? 'No completed tasks yet.' : 'All caught up! Hit + Add Task to plan your next topic.'}
            </p>
          </div>
        ) : (
          filteredTasks.map((task) => {
            const isDone = task.status === 'done';
            return (
              <div
                key={task.id}
                className={`group flex items-center justify-between gap-3 p-3.5 rounded-xl border transition-all ${
                  isDone
                    ? 'bg-zinc-900/30 border-zinc-800/60 opacity-60'
                    : 'bg-surface-card/70 border-surface-border hover:border-accent/40 shadow-sm'
                }`}
              >
                <div className="flex items-center gap-3 min-w-0 flex-1">
                  <button
                    onClick={() => handleToggle(task)}
                    className={`w-5 h-5 rounded-lg border flex items-center justify-center transition-all ${
                      isDone
                        ? 'bg-accent border-accent text-white'
                        : 'border-zinc-600 hover:border-accent bg-zinc-900'
                    }`}
                  >
                    {isDone && <Check className="w-3.5 h-3.5 stroke-[3]" />}
                  </button>

                  <div className="min-w-0 flex-1">
                    <p
                      className={`text-sm font-medium truncate ${
                        isDone ? 'line-through text-zinc-500' : 'text-zinc-100'
                      }`}
                    >
                      {task.title}
                    </p>
                    <div className="flex items-center gap-2 mt-1">
                      <Badge
                        variant={
                          task.priority === 'high'
                            ? 'red'
                            : task.priority === 'medium'
                            ? 'yellow'
                            : 'zinc'
                        }
                      >
                        {task.priority}
                      </Badge>
                      {task.estMin && (
                        <span className="text-[11px] text-zinc-500 flex items-center gap-1">
                          <Clock className="w-3 h-3" />
                          {task.estMin}m
                        </span>
                      )}
                    </div>
                  </div>
                </div>

                <button
                  onClick={() => onDeleteTask(task.id)}
                  className="text-zinc-600 hover:text-red-400 p-1.5 rounded-lg transition opacity-60 group-hover:opacity-100"
                  title="Delete task"
                >
                  <Trash2 className="w-4 h-4" />
                </button>
              </div>
            );
          })
        )}
      </div>

      {/* Add Task Modal */}
      <Modal isOpen={isModalOpen} onClose={() => setIsModalOpen(false)} title="Create New Study Task">
        <form onSubmit={handleCreate} className="space-y-4">
          <div className="space-y-1">
            <label className="text-xs font-semibold text-zinc-300">Task Title</label>
            <input
              type="text"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              placeholder="e.g. Master Round Robin Scheduling numericals"
              autoFocus
              className="w-full px-3.5 py-2 rounded-xl bg-zinc-900 border border-zinc-800 text-white placeholder-zinc-600 focus:outline-none focus:border-accent text-sm"
            />
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1">
              <label className="text-xs font-semibold text-zinc-300">Priority</label>
              <select
                value={priority}
                onChange={(e) => setPriority(e.target.value)}
                className="w-full px-3 py-2 rounded-xl bg-zinc-900 border border-zinc-800 text-white focus:outline-none focus:border-accent text-xs"
              >
                <option value="high">High Priority</option>
                <option value="medium">Medium Priority</option>
                <option value="low">Low Priority</option>
              </select>
            </div>

            <div className="space-y-1">
              <label className="text-xs font-semibold text-zinc-300">Estimated Duration</label>
              <div className="flex items-center gap-1">
                <input
                  type="number"
                  min="5"
                  max="240"
                  step="5"
                  value={estMin}
                  onChange={(e) => setEstMin(parseInt(e.target.value) || 0)}
                  className="w-full px-3 py-2 rounded-xl bg-zinc-900 border border-zinc-800 text-white text-xs"
                />
                <span className="text-xs text-zinc-500">min</span>
              </div>
            </div>
          </div>

          <div className="flex justify-end gap-2 pt-2">
            <Button type="button" variant="secondary" size="sm" onClick={() => setIsModalOpen(false)}>
              Cancel
            </Button>
            <Button type="submit" variant="primary" size="sm" disabled={loading || !title.trim()}>
              {loading ? 'Adding...' : 'Save Task'}
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
};
