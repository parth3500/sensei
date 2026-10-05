import React, { useState, useEffect, useRef } from 'react';
import {
  Plus,
  Check,
  Trash2,
  Clock,
  Calendar,
  CheckCircle2,
  Camera,
  UploadCloud,
  Sparkles,
  AlertCircle,
  FileImage,
  RefreshCw,
  X,
  Edit2
} from 'lucide-react';
import confetti from 'canvas-confetti';
import { TaskItem, ExtractedTaskItem } from '@/types';
import { api } from '@/services/api';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';
import { Badge } from '@/components/ui/Badge';
import { Modal } from '@/components/ui/Modal';

interface TodayViewProps {
  tasks: TaskItem[];
  onToggleTask: (task: TaskItem) => Promise<void>;
  onAddTask: (task: { title: string; priority: string; estMin?: number; dueDate?: string }) => Promise<void>;
  onDeleteTask: (id: number) => Promise<void>;
  onBatchAddTasks?: (tasks: Array<{ title: string; priority: string; estMin?: number; dueDate?: string }>) => Promise<void>;
}

export const TodayView: React.FC<TodayViewProps> = ({
  tasks,
  onToggleTask,
  onAddTask,
  onDeleteTask,
  onBatchAddTasks,
}) => {
  const [filter, setFilter] = useState<'all' | 'pending' | 'done'>('all');
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [title, setTitle] = useState('');
  const [priority, setPriority] = useState('medium');
  const [estMin, setEstMin] = useState<number>(30);
  const [loading, setLoading] = useState(false);

  // ── Photo Schedule Scanner State ──
  const [targetDay, setTargetDay] = useState<'today' | 'tomorrow' | 'custom'>('today');
  const [customDate, setCustomDate] = useState<string>(() => {
    const d = new Date();
    return d.toISOString().split('T')[0];
  });
  const [selectedImage, setSelectedImage] = useState<string | null>(null);
  const [imageFileName, setImageFileName] = useState<string>('');
  const [scanning, setScanning] = useState(false);
  const [scanError, setScanError] = useState<string | null>(null);
  const [extractedTasks, setExtractedTasks] = useState<ExtractedTaskItem[]>([]);
  const [isReviewOpen, setIsReviewOpen] = useState(false);
  const [isDragOver, setIsDragOver] = useState(false);
  const [savingBatch, setSavingBatch] = useState(false);
  const fileInputRef = useRef<HTMLInputElement | null>(null);

  // Helper to compute target date string (YYYY-MM-DD)
  const getComputedTargetDate = (): string => {
    const now = new Date();
    if (targetDay === 'today') {
      return now.toISOString().split('T')[0];
    } else if (targetDay === 'tomorrow') {
      const tomorrow = new Date(now);
      tomorrow.setDate(tomorrow.getDate() + 1);
      return tomorrow.toISOString().split('T')[0];
    }
    return customDate;
  };

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

  // ── Process Image File ──
  const processFile = (file: File) => {
    if (!file.type.startsWith('image/')) {
      setScanError('Please select a valid image file (PNG, JPG, JPEG, WEBP).');
      return;
    }
    setScanError(null);
    setImageFileName(file.name);
    const reader = new FileReader();
    reader.onload = (e) => {
      const base64 = e.target?.result as string;
      setSelectedImage(base64);
    };
    reader.readAsDataURL(file);
  };

  // ── Global Clipboard Paste Listener (Ctrl+V / Cmd+V) ──
  useEffect(() => {
    const handlePaste = (e: ClipboardEvent) => {
      // Don't intercept if user is typing in an input
      if (['INPUT', 'TEXTAREA'].includes((e.target as HTMLElement)?.tagName)) return;

      const items = e.clipboardData?.items;
      if (!items) return;

      for (let i = 0; i < items.length; i++) {
        if (items[i].type.startsWith('image/')) {
          const file = items[i].getAsFile();
          if (file) {
            processFile(file);
            break;
          }
        }
      }
    };

    window.addEventListener('paste', handlePaste);
    return () => window.removeEventListener('paste', handlePaste);
  }, []);

  // ── Run AI Schedule Scanner ──
  const handleScanSchedule = async () => {
    if (!selectedImage) return;

    setScanning(true);
    setScanError(null);
    const targetDateStr = getComputedTargetDate();

    try {
      const res = await api.scanScheduleImage(selectedImage, targetDateStr);
      if (res && res.tasks && res.tasks.length > 0) {
        const enriched = res.tasks.map((t) => ({
          ...t,
          dueDate: t.dueDate || targetDateStr,
          selected: true,
        }));
        setExtractedTasks(enriched);
        setIsReviewOpen(true);
      } else {
        setScanError('Could not extract any tasks from this image. Try another photo or add manually.');
      }
    } catch (err: any) {
      setScanError(err.message || 'Failed to scan image. Please check the image and try again.');
    } finally {
      setScanning(false);
    }
  };

  // ── Task Review Edits ──
  const handleUpdateExtractedTask = (index: number, updates: Partial<ExtractedTaskItem>) => {
    setExtractedTasks((prev) =>
      prev.map((t, i) => (i === index ? { ...t, ...updates } : t))
    );
  };

  const handleDeleteExtractedTask = (index: number) => {
    setExtractedTasks((prev) => prev.filter((_, i) => i !== index));
  };

  const handleAddCustomExtractedRow = () => {
    setExtractedTasks((prev) => [
      ...prev,
      {
        title: 'New Study Task',
        estMin: 30,
        priority: 'medium',
        dueDate: getComputedTargetDate(),
        selected: true,
      },
    ]);
  };

  // ── Save Confirmed Tasks to Task List ──
  const handleSaveVerifiedTasks = async () => {
    const selected = extractedTasks.filter((t) => t.selected && t.title.trim().length > 0);
    if (selected.length === 0) return;

    setSavingBatch(true);
    try {
      const payload = selected.map((t) => ({
        title: t.title.trim(),
        priority: t.priority || 'medium',
        estMin: t.estMin || 30,
        dueDate: t.dueDate || getComputedTargetDate(),
      }));

      if (onBatchAddTasks) {
        await onBatchAddTasks(payload);
      } else {
        // Fallback: use api.batchCreateTasks or sequential onAddTask
        try {
          await api.batchCreateTasks(payload);
          // Trigger individual adds to refresh local page state if needed
          for (const item of payload) {
            await onAddTask(item);
          }
        } catch {
          for (const item of payload) {
            await onAddTask(item);
          }
        }
      }

      confetti({
        particleCount: 100,
        spread: 70,
        origin: { y: 0.6 },
        colors: ['#7c5cff', '#10b981', '#f59e0b', '#ec4899', '#3b82f6'],
      });

      // Clear scanner state and close modal
      setIsReviewOpen(false);
      setSelectedImage(null);
      setImageFileName('');
      setExtractedTasks([]);
    } catch (err: any) {
      setScanError(`Failed to save tasks: ${err.message}`);
    } finally {
      setSavingBatch(false);
    }
  };

  return (
    <div className="space-y-6 pb-20">
      {/* ── SECTION 1 (AT THE VERY BEGINNING): PHOTO READING ABILITY IN TASK LIST ── */}
      <Card className="border-accent/40 bg-gradient-to-br from-zinc-900/90 via-surface-card to-zinc-950 p-5 space-y-4 shadow-lg ring-1 ring-accent/20">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-xl bg-accent/20 border border-accent/40 flex items-center justify-center text-accent shadow-inner">
              <Camera className="w-5 h-5" />
            </div>
            <div>
              <div className="flex items-center gap-2">
                <h3 className="text-base font-bold text-white tracking-tight">AI Schedule & Timetable Scanner</h3>
                <Badge variant="purple">AI Vision</Badge>
              </div>
              <p className="text-xs text-zinc-400">
                Upload or paste a photo of your schedule — AI extracts tasks automatically with full review before adding.
              </p>
            </div>
          </div>

          {/* Schedule Target Day Selector */}
          <div className="flex items-center gap-1.5 p-1 bg-zinc-900 border border-zinc-800 rounded-xl self-start sm:self-auto text-xs">
            <button
              onClick={() => setTargetDay('today')}
              className={`px-2.5 py-1 rounded-lg font-semibold transition ${
                targetDay === 'today'
                  ? 'bg-accent text-white shadow-sm'
                  : 'text-zinc-400 hover:text-white'
              }`}
            >
              Today
            </button>
            <button
              onClick={() => setTargetDay('tomorrow')}
              className={`px-2.5 py-1 rounded-lg font-semibold transition ${
                targetDay === 'tomorrow'
                  ? 'bg-accent text-white shadow-sm'
                  : 'text-zinc-400 hover:text-white'
              }`}
            >
              Tomorrow
            </button>
            <button
              onClick={() => setTargetDay('custom')}
              className={`px-2.5 py-1 rounded-lg font-semibold transition ${
                targetDay === 'custom'
                  ? 'bg-accent text-white shadow-sm'
                  : 'text-zinc-400 hover:text-white'
              }`}
            >
              Custom Date
            </button>
          </div>
        </div>

        {/* Custom Date Input (shown if Custom selected) */}
        {targetDay === 'custom' && (
          <div className="flex items-center gap-2 pt-1 animate-fade-in">
            <span className="text-xs text-zinc-400 font-medium">Target Date:</span>
            <input
              type="date"
              value={customDate}
              onChange={(e) => setCustomDate(e.target.value)}
              className="px-3 py-1.5 rounded-lg bg-zinc-900 border border-zinc-700 text-xs text-white focus:outline-none focus:border-accent"
            />
          </div>
        )}

        {/* Drag & Drop Upload Zone */}
        <div
          onDragOver={(e) => {
            e.preventDefault();
            setIsDragOver(true);
          }}
          onDragLeave={() => setIsDragOver(false)}
          onDrop={(e) => {
            e.preventDefault();
            setIsDragOver(false);
            if (e.dataTransfer.files && e.dataTransfer.files[0]) {
              processFile(e.dataTransfer.files[0]);
            }
          }}
          onClick={() => fileInputRef.current?.click()}
          className={`cursor-pointer border-2 border-dashed rounded-xl p-4 transition-all flex flex-col items-center justify-center gap-2 text-center ${
            isDragOver
              ? 'border-accent bg-accent/10'
              : selectedImage
              ? 'border-emerald-500/60 bg-emerald-500/5'
              : 'border-zinc-700/70 hover:border-zinc-500 bg-zinc-900/40 hover:bg-zinc-900/70'
          }`}
        >
          <input
            ref={fileInputRef}
            type="file"
            accept="image/*"
            className="hidden"
            onChange={(e) => {
              if (e.target.files && e.target.files[0]) {
                processFile(e.target.files[0]);
              }
            }}
          />

          {selectedImage ? (
            <div className="flex items-center gap-3 w-full max-w-md p-2 bg-zinc-900/90 rounded-lg border border-zinc-800">
              {/* Image preview thumbnail */}
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img
                src={selectedImage}
                alt="Schedule Preview"
                className="w-14 h-14 object-cover rounded-lg border border-zinc-700 shrink-0"
              />
              <div className="min-w-0 flex-1 text-left">
                <p className="text-xs font-semibold text-white truncate">{imageFileName || 'Pasted Schedule Image'}</p>
                <p className="text-[11px] text-emerald-400 flex items-center gap-1">
                  <Check className="w-3 h-3 stroke-[3]" /> Photo ready for AI extraction
                </p>
                <span className="text-[10px] text-zinc-500">Targeting {getComputedTargetDate()}</span>
              </div>
              <button
                type="button"
                onClick={(e) => {
                  e.stopPropagation();
                  setSelectedImage(null);
                  setImageFileName('');
                  setExtractedTasks([]);
                }}
                className="p-1.5 text-zinc-500 hover:text-red-400 rounded-lg transition"
                title="Remove photo"
              >
                <X className="w-4 h-4" />
              </button>
            </div>
          ) : (
            <div className="space-y-1.5 py-2">
              <UploadCloud className="w-8 h-8 text-accent mx-auto animate-bounce" />
              <p className="text-xs font-medium text-zinc-200">
                <span className="text-accent font-semibold">Click to upload</span> or drag and drop your schedule photo
              </p>
              <p className="text-[11px] text-zinc-500">
                Tip: You can also press <kbd className="px-1.5 py-0.5 rounded bg-zinc-800 text-zinc-300 font-mono text-[10px]">Ctrl+V</kbd> to paste a screenshot directly!
              </p>
            </div>
          )}
        </div>

        {/* Action Button & Errors */}
        {scanError && (
          <div className="flex items-center gap-2 p-2.5 rounded-lg bg-rose-500/10 border border-rose-500/30 text-rose-300 text-xs">
            <AlertCircle className="w-4 h-4 shrink-0 text-rose-400" />
            <span>{scanError}</span>
          </div>
        )}

        {selectedImage && (
          <div className="flex items-center justify-end gap-2 pt-1">
            <Button
              onClick={handleScanSchedule}
              disabled={scanning}
              size="sm"
              className="bg-accent hover:bg-accent/90 text-white shadow-md"
            >
              {scanning ? (
                <>
                  <RefreshCw className="w-4 h-4 mr-1.5 animate-spin" />
                  Reading Schedule with AI...
                </>
              ) : (
                <>
                  <Sparkles className="w-4 h-4 mr-1.5 text-yellow-300" />
                  Extract Tasks from Photo
                </>
              )}
            </Button>
          </div>
        )}
      </Card>

      {/* ── INTERACTIVE EXTRACTED TASKS VERIFICATION MODAL ── */}
      <Modal
        isOpen={isReviewOpen}
        onClose={() => setIsReviewOpen(false)}
        title="Review & Customize Extracted Schedule Tasks"
      >
        <div className="space-y-4 max-h-[70vh] overflow-y-auto pr-1">
          <div className="p-3 rounded-xl bg-accent/10 border border-accent/20 text-xs text-zinc-300 space-y-1">
            <div className="flex items-center gap-1.5 text-accent font-bold">
              <Sparkles className="w-4 h-4" />
              <span>{extractedTasks.filter((t) => t.selected).length} Tasks Detected from Schedule</span>
            </div>
            <p className="text-zinc-400 text-[11px]">
              AI automatically extracted these items. You can edit any titles, durations, or priorities that were misread before saving to your study plan.
            </p>
          </div>

          {/* List of Extracted Tasks for Editing */}
          <div className="space-y-3">
            {extractedTasks.map((t, idx) => (
              <div
                key={idx}
                className={`p-3.5 rounded-xl border transition-all space-y-2.5 ${
                  t.selected
                    ? 'bg-zinc-900/80 border-accent/40 shadow-sm'
                    : 'bg-zinc-900/30 border-zinc-800 opacity-50'
                }`}
              >
                <div className="flex items-center gap-3">
                  <input
                    type="checkbox"
                    checked={!!t.selected}
                    onChange={(e) => handleUpdateExtractedTask(idx, { selected: e.target.checked })}
                    className="w-4 h-4 rounded text-accent accent-accent cursor-pointer"
                  />
                  <div className="flex-1">
                    <input
                      type="text"
                      value={t.title}
                      onChange={(e) => handleUpdateExtractedTask(idx, { title: e.target.value })}
                      placeholder="Task description"
                      className="w-full px-2.5 py-1.5 rounded-lg bg-zinc-950 border border-zinc-800 text-white text-xs focus:outline-none focus:border-accent font-medium"
                    />
                  </div>
                  <button
                    onClick={() => handleDeleteExtractedTask(idx)}
                    className="p-1 text-zinc-600 hover:text-red-400 transition"
                    title="Remove item"
                  >
                    <Trash2 className="w-3.5 h-3.5" />
                  </button>
                </div>

                <div className="grid grid-cols-3 gap-2 pl-7 text-xs">
                  <div>
                    <label className="text-[10px] text-zinc-500 font-semibold block mb-0.5">Priority</label>
                    <select
                      value={t.priority}
                      onChange={(e) => handleUpdateExtractedTask(idx, { priority: e.target.value })}
                      className="w-full px-2 py-1 rounded-lg bg-zinc-950 border border-zinc-800 text-white text-[11px] focus:outline-none focus:border-accent"
                    >
                      <option value="high">High</option>
                      <option value="medium">Medium</option>
                      <option value="low">Low</option>
                    </select>
                  </div>

                  <div>
                    <label className="text-[10px] text-zinc-500 font-semibold block mb-0.5">Duration</label>
                    <div className="flex items-center gap-1">
                      <input
                        type="number"
                        min="5"
                        max="240"
                        step="5"
                        value={t.estMin || 30}
                        onChange={(e) =>
                          handleUpdateExtractedTask(idx, { estMin: parseInt(e.target.value) || 30 })
                        }
                        className="w-full px-2 py-1 rounded-lg bg-zinc-950 border border-zinc-800 text-white text-[11px] focus:outline-none focus:border-accent"
                      />
                      <span className="text-[10px] text-zinc-500">m</span>
                    </div>
                  </div>

                  <div>
                    <label className="text-[10px] text-zinc-500 font-semibold block mb-0.5">Due Date</label>
                    <input
                      type="date"
                      value={t.dueDate || getComputedTargetDate()}
                      onChange={(e) => handleUpdateExtractedTask(idx, { dueDate: e.target.value })}
                      className="w-full px-2 py-1 rounded-lg bg-zinc-950 border border-zinc-800 text-white text-[11px] focus:outline-none focus:border-accent"
                    />
                  </div>
                </div>
              </div>
            ))}
          </div>

          <div className="flex items-center justify-between pt-2">
            <Button
              type="button"
              variant="secondary"
              size="sm"
              onClick={handleAddCustomExtractedRow}
              className="text-xs"
            >
              <Plus className="w-3.5 h-3.5 mr-1" />
              Add Another Item
            </Button>

            <div className="flex items-center gap-2">
              <Button
                type="button"
                variant="ghost"
                size="sm"
                onClick={() => setIsReviewOpen(false)}
              >
                Discard
              </Button>
              <Button
                type="button"
                variant="primary"
                size="sm"
                disabled={savingBatch || extractedTasks.filter((t) => t.selected).length === 0}
                onClick={handleSaveVerifiedTasks}
              >
                {savingBatch ? 'Adding Tasks...' : `Add ${extractedTasks.filter((t) => t.selected).length} Tasks to Sensei`}
              </Button>
            </div>
          </div>
        </div>
      </Modal>

      {/* Header & Quick Add */}
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-extrabold text-white tracking-tight">Today&apos;s Focus</h2>
          <p className="text-xs text-zinc-400">
            {new Date().toLocaleDateString('en-IN', { weekday: 'long', month: 'short', day: 'numeric' })}
          </p>
        </div>
        <Button onClick={() => setIsModalOpen(true)} size="sm">
          <Plus className="w-4 h-4 mr-1" />
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
              {filter === 'done' ? 'No completed tasks yet.' : 'All caught up! Scan a schedule photo or hit + Add Task to plan your next topic.'}
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

      {/* Manual Add Task Modal */}
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
