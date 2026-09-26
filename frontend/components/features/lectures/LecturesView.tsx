import React, { useState } from 'react';
import { Video, Plus, CheckCircle, Play, Clock, ExternalLink, Trash2, Edit3 } from 'lucide-react';
import { LectureItem } from '@/types';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';
import { Badge } from '@/components/ui/Badge';
import { Modal } from '@/components/ui/Modal';

interface LecturesViewProps {
  lectures: LectureItem[];
  onAddLecture: (data: { title: string; videoUrl: string; totalDurationSec?: number }) => Promise<void>;
  onUpdateProgress: (id: number, data: { currentTimeSec: number; totalDurationSec?: number; completed?: boolean; notes?: string }) => Promise<void>;
  onDeleteLecture: (id: number) => Promise<void>;
}

function formatTime(seconds: number): string {
  const m = Math.floor(seconds / 60);
  const s = seconds % 60;
  return `${m}:${s < 10 ? '0' : ''}${s}`;
}

export const LecturesView: React.FC<LecturesViewProps> = ({
  lectures,
  onAddLecture,
  onUpdateProgress,
  onDeleteLecture,
}) => {
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [selectedLecture, setSelectedLecture] = useState<LectureItem | null>(null);

  // Form states
  const [title, setTitle] = useState('');
  const [url, setUrl] = useState('');
  const [durationMin, setDurationMin] = useState(45);
  const [loading, setLoading] = useState(false);

  // Inline progress update states
  const [currentMinutes, setCurrentMinutes] = useState(0);
  const [lectureNotes, setLectureNotes] = useState('');

  const handleSelectLecture = (lec: LectureItem) => {
    setSelectedLecture(lec);
    setCurrentMinutes(Math.floor(lec.currentTimeSec / 60));
    setLectureNotes(lec.notes || '');
  };

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!title.trim() || !url.trim()) return;

    setLoading(true);
    try {
      await onAddLecture({
        title: title.trim(),
        videoUrl: url.trim(),
        totalDurationSec: durationMin * 60,
      });
      setTitle('');
      setUrl('');
      setDurationMin(45);
      setIsModalOpen(false);
    } finally {
      setLoading(false);
    }
  };

  const handleSaveProgress = async () => {
    if (!selectedLecture) return;
    const timeSec = currentMinutes * 60;
    await onUpdateProgress(selectedLecture.id, {
      currentTimeSec: timeSec,
      notes: lectureNotes,
    });
    setSelectedLecture({
      ...selectedLecture,
      currentTimeSec: timeSec,
      notes: lectureNotes,
    });
  };

  const handleToggleComplete = async (lec: LectureItem) => {
    await onUpdateProgress(lec.id, {
      currentTimeSec: lec.completed ? 0 : lec.totalDurationSec,
      completed: !lec.completed,
    });
    if (selectedLecture?.id === lec.id) {
      setSelectedLecture({ ...selectedLecture, completed: !lec.completed });
    }
  };

  return (
    <div className="space-y-6 pb-20">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-extrabold text-white tracking-tight">Lecture Tracker</h2>
          <p className="text-xs text-zinc-400">Track video progress, timestamps &amp; notes</p>
        </div>
        <Button onClick={() => setIsModalOpen(true)} size="sm">
          <Plus className="w-4 h-4" />
          Add Video Link
        </Button>
      </div>

      {/* Active Lecture Player / Progress Box */}
      {selectedLecture && (
        <Card className="space-y-4 border-accent/40 bg-surface-card/90">
          <div className="flex justify-between items-start gap-2">
            <div>
              <span className="text-[10px] font-bold text-accent uppercase tracking-wider">Active Lecture</span>
              <h3 className="text-base font-bold text-white">{selectedLecture.title}</h3>
            </div>
            <a
              href={selectedLecture.videoUrl}
              target="_blank"
              rel="noopener noreferrer"
              className="p-1.5 rounded-lg bg-zinc-800 text-zinc-300 hover:text-white hover:bg-zinc-700 transition"
              title="Open Video in New Tab"
            >
              <ExternalLink className="w-4 h-4" />
            </a>
          </div>

          {/* YouTube Embed if available */}
          {selectedLecture.videoId ? (
            <div className="relative w-full aspect-video rounded-xl overflow-hidden border border-surface-border bg-black">
              <iframe
                src={`https://www.youtube.com/embed/${selectedLecture.videoId}?start=${selectedLecture.currentTimeSec}`}
                title={selectedLecture.title}
                className="w-full h-full"
                allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
                allowFullScreen
              />
            </div>
          ) : (
            <div className="p-4 rounded-xl bg-zinc-900 border border-surface-border flex items-center justify-between">
              <span className="text-xs text-zinc-400 truncate max-w-sm">{selectedLecture.videoUrl}</span>
              <a
                href={selectedLecture.videoUrl}
                target="_blank"
                rel="noreferrer"
                className="text-xs text-accent font-semibold flex items-center gap-1"
              >
                Watch Video <ExternalLink className="w-3.5 h-3.5" />
              </a>
            </div>
          )}

          {/* Timestamp tracker control */}
          <div className="p-3.5 rounded-xl bg-zinc-900/80 border border-surface-border space-y-3">
            <div className="flex justify-between items-center text-xs">
              <span className="text-zinc-400 font-medium">Watched Timestamp:</span>
              <span className="text-accent font-bold">
                {formatTime(currentMinutes * 60)} / {formatTime(selectedLecture.totalDurationSec)} (
                {selectedLecture.totalDurationSec > 0
                  ? Math.round(((currentMinutes * 60) / selectedLecture.totalDurationSec) * 100)
                  : 0}
                %)
              </span>
            </div>

            <div className="flex items-center gap-2">
              <input
                type="range"
                min="0"
                max={Math.floor(selectedLecture.totalDurationSec / 60)}
                value={currentMinutes}
                onChange={(e) => setCurrentMinutes(parseInt(e.target.value) || 0)}
                className="w-full accent-accent h-2 bg-zinc-800 rounded-lg cursor-pointer"
              />
            </div>

            <div className="flex items-center justify-between pt-1">
              <div className="flex items-center gap-1.5">
                <button
                  onClick={() => setCurrentMinutes(Math.max(0, currentMinutes - 5))}
                  className="px-2 py-1 rounded bg-zinc-800 text-[11px] text-zinc-300 hover:bg-zinc-700"
                >
                  -5m
                </button>
                <button
                  onClick={() => setCurrentMinutes(currentMinutes + 5)}
                  className="px-2 py-1 rounded bg-zinc-800 text-[11px] text-zinc-300 hover:bg-zinc-700"
                >
                  +5m
                </button>
                <button
                  onClick={() => handleToggleComplete(selectedLecture)}
                  className={`px-2.5 py-1 rounded text-[11px] font-semibold flex items-center gap-1 transition ${
                    selectedLecture.completed
                      ? 'bg-emerald-500/20 text-emerald-300 border border-emerald-500/40'
                      : 'bg-zinc-800 text-zinc-400'
                  }`}
                >
                  <CheckCircle className="w-3 h-3" />
                  {selectedLecture.completed ? 'Completed' : 'Mark Complete'}
                </button>
              </div>

              <Button onClick={handleSaveProgress} size="sm">
                Save Progress
              </Button>
            </div>
          </div>

          {/* Notes for lecture */}
          <div className="space-y-1.5">
            <label className="text-xs font-semibold text-zinc-300 flex items-center gap-1.5">
              <Edit3 className="w-3.5 h-3.5 text-accent" />
              Timestamp Notes &amp; Formulas:
            </label>
            <textarea
              value={lectureNotes}
              onChange={(e) => setLectureNotes(e.target.value)}
              placeholder="e.g. At 18:20: formula for Average Waiting Time in Round Robin. Trap: context switch overhead."
              rows={2}
              className="w-full p-2.5 rounded-xl bg-zinc-900 border border-surface-border text-xs text-white placeholder-zinc-600 focus:outline-none focus:border-accent"
            />
          </div>
        </Card>
      )}

      {/* Lectures List */}
      <div className="space-y-2.5">
        <h3 className="text-sm font-bold text-zinc-300">Course Lectures</h3>

        {lectures.length === 0 ? (
          <div className="p-8 text-center rounded-2xl border border-dashed border-zinc-800 bg-surface-card/30">
            <Video className="w-8 h-8 text-zinc-600 mx-auto mb-2" />
            <p className="text-xs text-zinc-400 font-medium">No video lectures added yet. Hit + Add Video Link to start tracking.</p>
          </div>
        ) : (
          lectures.map((lec) => {
            const isSelected = selectedLecture?.id === lec.id;
            const pct =
              lec.totalDurationSec > 0
                ? Math.min(100, Math.round((lec.currentTimeSec / lec.totalDurationSec) * 100))
                : 0;

            return (
              <div
                key={lec.id}
                onClick={() => handleSelectLecture(lec)}
                className={`p-3.5 rounded-xl border flex items-center justify-between gap-3 cursor-pointer transition ${
                  isSelected
                    ? 'border-accent bg-accent/10 shadow-md'
                    : 'border-surface-border bg-surface-card/60 hover:border-zinc-700'
                }`}
              >
                <div className="flex items-center gap-3 min-w-0 flex-1">
                  <div
                    className={`w-9 h-9 rounded-xl flex items-center justify-center shrink-0 ${
                      lec.completed
                        ? 'bg-emerald-500/20 text-emerald-400 border border-emerald-500/30'
                        : 'bg-zinc-800 text-accent'
                    }`}
                  >
                    {lec.completed ? <CheckCircle className="w-5 h-5" /> : <Play className="w-4 h-4" />}
                  </div>

                  <div className="min-w-0 flex-1">
                    <p className="text-sm font-semibold text-white truncate">{lec.title}</p>
                    <div className="flex items-center gap-2 mt-1">
                      <span className="text-[11px] text-zinc-400 flex items-center gap-1 font-mono">
                        <Clock className="w-3 h-3 text-zinc-500" />
                        {formatTime(lec.currentTimeSec)} / {formatTime(lec.totalDurationSec)}
                      </span>
                      <Badge variant={lec.completed ? 'green' : pct > 0 ? 'purple' : 'zinc'}>
                        {lec.completed ? 'Finished' : `${pct}%`}
                      </Badge>
                    </div>
                  </div>
                </div>

                <div className="flex items-center gap-1">
                  <button
                    onClick={(e) => {
                      e.stopPropagation();
                      onDeleteLecture(lec.id);
                    }}
                    className="p-1.5 text-zinc-600 hover:text-red-400 rounded-lg transition"
                    title="Delete"
                  >
                    <Trash2 className="w-4 h-4" />
                  </button>
                </div>
              </div>
            );
          })
        )}
      </div>

      {/* Add Lecture Modal */}
      <Modal isOpen={isModalOpen} onClose={() => setIsModalOpen(false)} title="Add Video Lecture Link">
        <form onSubmit={handleCreate} className="space-y-4">
          <div className="space-y-1">
            <label className="text-xs font-semibold text-zinc-300">Lecture Title</label>
            <input
              type="text"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              placeholder="e.g. Gate Smashers: Process Synchronization (Peterson Solution)"
              autoFocus
              className="w-full px-3.5 py-2 rounded-xl bg-zinc-900 border border-zinc-800 text-white placeholder-zinc-600 focus:outline-none focus:border-accent text-sm"
            />
          </div>

          <div className="space-y-1">
            <label className="text-xs font-semibold text-zinc-300">Video Link / YouTube URL</label>
            <input
              type="url"
              value={url}
              onChange={(e) => setUrl(e.target.value)}
              placeholder="https://www.youtube.com/watch?v=..."
              className="w-full px-3.5 py-2 rounded-xl bg-zinc-900 border border-zinc-800 text-white placeholder-zinc-600 focus:outline-none focus:border-accent text-sm"
            />
          </div>

          <div className="space-y-1">
            <label className="text-xs font-semibold text-zinc-300">Total Duration (approx minutes)</label>
            <input
              type="number"
              min="5"
              max="360"
              value={durationMin}
              onChange={(e) => setDurationMin(parseInt(e.target.value) || 45)}
              className="w-full px-3.5 py-2 rounded-xl bg-zinc-900 border border-zinc-800 text-white text-sm"
            />
          </div>

          <div className="flex justify-end gap-2 pt-2">
            <Button type="button" variant="secondary" size="sm" onClick={() => setIsModalOpen(false)}>
              Cancel
            </Button>
            <Button type="submit" variant="primary" size="sm" disabled={loading || !title.trim() || !url.trim()}>
              {loading ? 'Adding...' : 'Track Lecture'}
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
};
