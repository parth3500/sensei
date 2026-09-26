import React, { useState } from 'react';
import { FolderGit2, Upload, FileText, CheckCircle2, Database, Sparkles, BookOpen } from 'lucide-react';
import { DriveFileItem, DriveProcessResult } from '@/types';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';
import { Badge } from '@/components/ui/Badge';

interface DriveStorageViewProps {
  driveStatus: any;
  files: DriveFileItem[];
  onIngestFile: (data: { fileName: string; content: string }) => Promise<DriveProcessResult>;
  onBackup: () => Promise<{ success: boolean; backupPath: string }>;
}

export const DriveStorageView: React.FC<DriveStorageViewProps> = ({
  driveStatus,
  files,
  onIngestFile,
  onBackup,
}) => {
  const [fileName, setFileName] = useState('');
  const [content, setContent] = useState('');
  const [loading, setLoading] = useState(false);
  const [backupLoading, setBackupLoading] = useState(false);
  const [lastResult, setLastResult] = useState<DriveProcessResult | null>(null);
  const [backupMsg, setBackupMsg] = useState<string | null>(null);

  const handleIngest = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!fileName.trim() || !content.trim()) return;

    setLoading(true);
    setLastResult(null);
    try {
      const res = await onIngestFile({
        fileName: fileName.trim(),
        content: content.trim(),
      });
      setLastResult(res);
      setFileName('');
      setContent('');
    } finally {
      setLoading(false);
    }
  };

  const handleBackupNow = async () => {
    setBackupLoading(true);
    setBackupMsg(null);
    try {
      const res = await onBackup();
      setBackupMsg(`Database snapshot successfully backed up to ${res.backupPath}`);
    } catch {
      setBackupMsg('Failed to backup database.');
    } finally {
      setBackupLoading(false);
    }
  };

  return (
    <div className="space-y-6 pb-20">
      {/* Header */}
      <div>
        <h2 className="text-2xl font-extrabold text-white tracking-tight">Google Drive Storage &amp; Ingest</h2>
        <p className="text-xs text-zinc-400">
          Auto-summarize notes, extract key revision points, and generate practice questions
        </p>
      </div>

      {/* Drive Status & Backup Card */}
      <Card className="space-y-3.5 bg-surface-card/80">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2.5">
            <div className="w-8 h-8 rounded-xl bg-blue-500/10 text-blue-400 border border-blue-500/20 flex items-center justify-center">
              <FolderGit2 className="w-4 h-4" />
            </div>
            <div>
              <h3 className="text-sm font-bold text-white">Drive Folder: GATE_Sensei_Notes/</h3>
              <p className="text-[11px] text-zinc-500">Live sync folder for revision documents and database snapshots</p>
            </div>
          </div>
          <Badge variant="green">Active</Badge>
        </div>

        <div className="flex items-center justify-between pt-2 border-t border-zinc-800">
          <div className="text-xs text-zinc-400">
            <span>Durable persistence: </span>
            <span className="text-zinc-200 font-mono">SQLite WAL + Drive Backup</span>
          </div>
          <Button onClick={handleBackupNow} variant="secondary" size="sm" disabled={backupLoading}>
            <Database className="w-3.5 h-3.5 text-accent" />
            {backupLoading ? 'Backing up...' : 'Backup DB Now'}
          </Button>
        </div>

        {backupMsg && (
          <p className="text-xs text-emerald-400 p-2.5 rounded-xl bg-emerald-500/10 border border-emerald-500/20">
            {backupMsg}
          </p>
        )}
      </Card>

      {/* Ingest Note Box */}
      <Card className="space-y-4">
        <div className="flex items-center gap-2">
          <Upload className="w-4 h-4 text-accent" />
          <h3 className="text-sm font-bold text-white">Drop or Paste Study Note from Drive</h3>
        </div>

        <form onSubmit={handleIngest} className="space-y-3">
          <div>
            <label className="text-xs font-semibold text-zinc-300">File Name (with topic)</label>
            <input
              type="text"
              value={fileName}
              onChange={(e) => setFileName(e.target.value)}
              placeholder="e.g. Computer_Networks_TCP_Congestion.txt"
              className="w-full mt-1 px-3.5 py-2 rounded-xl bg-zinc-900 border border-surface-border text-white text-xs placeholder-zinc-600 focus:outline-none focus:border-accent"
            />
          </div>

          <div>
            <label className="text-xs font-semibold text-zinc-300">Note Content / Transcript</label>
            <textarea
              value={content}
              onChange={(e) => setContent(e.target.value)}
              placeholder="Paste summary, lecture notes, textbook excerpt, or formulas..."
              rows={4}
              className="w-full mt-1 p-3 rounded-xl bg-zinc-900 border border-surface-border text-xs text-white placeholder-zinc-600 focus:outline-none focus:border-accent"
            />
          </div>

          <Button type="submit" disabled={loading || !fileName.trim() || !content.trim()} className="w-full">
            <Sparkles className="w-4 h-4" />
            {loading ? 'Processing & Generating Questions...' : 'Ingest File & Generate Questions'}
          </Button>
        </form>
      </Card>

      {/* Extracted Result Preview */}
      {lastResult && (
        <Card className="space-y-4 border-emerald-500/40 bg-emerald-500/5 animate-fade-in">
          <div className="flex items-center gap-2 text-emerald-400 font-bold text-sm">
            <CheckCircle2 className="w-4 h-4" />
            Extraction Complete for {lastResult.fileName}
          </div>

          <div className="space-y-2 text-xs">
            <p className="font-semibold text-zinc-300">Quick Summary:</p>
            <p className="text-zinc-300 bg-zinc-900/60 p-3 rounded-xl border border-surface-border">
              {lastResult.summary}
            </p>

            <p className="font-semibold text-zinc-300 pt-2">Key Revision Takeaways:</p>
            <ul className="list-disc list-inside space-y-1 text-zinc-300 bg-zinc-900/60 p-3 rounded-xl border border-surface-border">
              {lastResult.revisionPoints.map((pt, idx) => (
                <li key={idx}>{pt}</li>
              ))}
            </ul>

            <div className="p-3 rounded-xl bg-purple-500/10 border border-purple-500/20 text-purple-300 text-xs flex items-center justify-between">
              <span>{lastResult.generatedQuestions.length} Practice questions generated &amp; added to your question bank!</span>
              <BookOpen className="w-4 h-4 text-accent" />
            </div>
          </div>
        </Card>
      )}

      {/* Ingested Files List */}
      <div className="space-y-2.5">
        <h3 className="text-sm font-bold text-zinc-300">Previously Ingested Drive Documents</h3>
        {files.length === 0 ? (
          <p className="text-xs text-zinc-500 py-3 text-center">No files ingested yet.</p>
        ) : (
          files.map((file) => (
            <div
              key={file.id}
              className="p-3.5 rounded-xl border border-surface-border bg-surface-card/60 flex items-center justify-between gap-3 text-xs"
            >
              <div className="flex items-center gap-2.5 min-w-0 flex-1">
                <FileText className="w-4 h-4 text-accent shrink-0" />
                <div className="truncate">
                  <p className="font-semibold text-zinc-200 truncate">{file.fileName}</p>
                  <p className="text-[10px] text-zinc-500">{file.folderPath}</p>
                </div>
              </div>
              <Badge variant="purple">{file.generatedQuestionsCount} Qs Added</Badge>
            </div>
          ))
        )}
      </div>
    </div>
  );
};
